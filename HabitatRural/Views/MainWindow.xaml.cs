using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Win32;
using HabitatRural.Models;
using HabitatRural.Services;

namespace HabitatRural.Views;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<string> _logs = new();
    private readonly PdfFiller _filler = new();

    public MainWindow()
    {
        InitializeComponent();
        LogList.ItemsSource = _logs;

        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // التواريخ الافتراضية
        DpDateDecision.SelectedDate = DateTime.Today;
        DpDatePermis.SelectedDate   = DateTime.Today;
        // التواريخ الاختيارية تبقى null
        DpDateDemande.SelectedDate = null;
        DpDateVisite.SelectedDate  = null;
        DpDatePV.SelectedDate      = null;

        RefreshSubdivDropdown();
        RefreshBeneficiairesDropdown();
        UpdateLast5();
        RecomputeLogic();
        UpdateTemplateLabel();

        // إن كان أول تشغيل، نفتح الإعدادات مباشرة
        if (SettingsService.Instance.Current.FirstRun)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                ShowToast("👋 مرحباً! يرجى ضبط الإعدادات العامة في أول تشغيل", ToastType.Warn);
                OpenSettings();
            }), DispatcherPriority.Background);
        }
    }

    // ════════════════════ Toast ════════════════════
    private enum ToastType { Success, Warn, Error }
    private DispatcherTimer? _toastTimer;
    private void ShowToast(string msg, ToastType type = ToastType.Success)
    {
        TxtToast.Text = msg;
        Toast.Background = type switch
        {
            ToastType.Warn  => (System.Windows.Media.Brush)FindResource("WarnBrush"),
            ToastType.Error => (System.Windows.Media.Brush)FindResource("DangerBrush"),
            _               => (System.Windows.Media.Brush)FindResource("SuccessBrush"),
        };
        Toast.Visibility = Visibility.Visible;
        _toastTimer?.Stop();
        _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3.5) };
        _toastTimer.Tick += (_, __) => { Toast.Visibility = Visibility.Collapsed; _toastTimer!.Stop(); };
        _toastTimer.Start();
    }

    // ════════════════════ Beneficiaires Dropdown ════════════════════
    private void RefreshBeneficiairesDropdown()
    {
        var prev = CbExistingBeneficiaires.SelectedItem as Beneficiaire;
        CbExistingBeneficiaires.ItemsSource = null;
        var list = new ObservableCollection<Beneficiaire>(BeneficiairesRepository.Instance.All);
        CbExistingBeneficiaires.ItemsSource = list;
        if (prev != null) CbExistingBeneficiaires.SelectedItem =
            list.FirstOrDefault(x => x.Id == prev.Id);
    }

    private void CbExistingBeneficiaires_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CbExistingBeneficiaires.SelectedItem is Beneficiaire b)
        {
            TxtCode.Text       = b.Code;
            TxtNomPrenom.Text  = b.NomPrenom;
            TxtAdresse.Text    = b.Adresse;
            TxtCcp.Text        = b.NumeroCcp;
            DpDateDecision.SelectedDate = b.DateDecision;
            UpdateLast5();
            ShowToast($"📂 تم تحميل المستفيد: {b.NomPrenom}");
        }
    }

    private void BtnSaveBeneficiaire_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateShared()) return;
        var b = BuildBeneficiaireFromForm();
        // محاولة التحديث إن كان موجوداً بنفس الـ Code
        var existing = BeneficiairesRepository.Instance.All
            .FirstOrDefault(x => string.Equals(x.Code, b.Code, StringComparison.OrdinalIgnoreCase));
        if (existing != null) b.Id = existing.Id;
        BeneficiairesRepository.Instance.Save(b);
        RefreshBeneficiairesDropdown();
        ShowToast(existing != null ? "✅ تم تحديث بيانات المستفيد" : "✅ تمت إضافة مستفيد جديد");
    }

    private void BtnDeleteBeneficiaire_Click(object sender, RoutedEventArgs e)
    {
        if (CbExistingBeneficiaires.SelectedItem is not Beneficiaire b)
        {
            ShowToast("⚠ اختر مستفيداً من القائمة أولاً", ToastType.Warn);
            return;
        }
        var ok = MessageBox.Show(
            $"هل تريد حذف المستفيد:\n{b.NomPrenom}؟",
            "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (ok != MessageBoxResult.Yes) return;
        BeneficiairesRepository.Instance.Delete(b.Id);
        RefreshBeneficiairesDropdown();
        ShowToast("🗑 تم الحذف", ToastType.Warn);
    }

    private Beneficiaire BuildBeneficiaireFromForm() => new()
    {
        Code         = TxtCode.Text.Trim(),
        NomPrenom    = TxtNomPrenom.Text.Trim(),
        Adresse      = TxtAdresse.Text.Trim(),
        NumeroCcp    = TxtCcp.Text.Trim(),
        DateDecision = DpDateDecision.SelectedDate ?? DateTime.Today,
    };

    // ════════════════════ Subdiv ════════════════════
    private void RefreshSubdivDropdown()
    {
        var prev = CbSubdiv.Text;
        CbSubdiv.ItemsSource = null;
        CbSubdiv.ItemsSource = SettingsService.Instance.Current.Subdivisionnaires;
        if (!string.IsNullOrEmpty(prev) &&
            SettingsService.Instance.Current.Subdivisionnaires.Contains(prev))
            CbSubdiv.Text = prev;
        else if (SettingsService.Instance.Current.Subdivisionnaires.Count > 0)
            CbSubdiv.Text = SettingsService.Instance.Current.Subdivisionnaires[0];
    }

    // ════════════════════ Logic ════════════════════
    private void TxtCode_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateLast5();
        RecomputeLogic();
    }

    private void UpdateLast5()
    {
        var code = (TxtCode?.Text ?? "").Trim();
        TxtLast5.Text = code.Length >= 5 ? code[^5..] : code;
    }

    private void Logic_Changed(object sender, RoutedEventArgs e) => RecomputeLogic();

    private TrancheType GetTranche()
        => RbT1.IsChecked == true ? TrancheType.T1Only
        :  RbT2.IsChecked == true ? TrancheType.T2Only
        :                           TrancheType.Both;

    private BuildingType GetBuilding()
        => RbTalia.IsChecked == true ? BuildingType.Talia : BuildingType.Ardhi;

    private void RecomputeLogic()
    {
        if (TxtLogicSummary == null) return;
        var req = new AnnexeRequest
        {
            Beneficiaire = BuildBeneficiaireFromForm(),
            Tranche      = GetTranche(),
            Building     = GetBuilding(),
        };
        var logic = LogicService.Compute(SettingsService.Instance.Current, req);
        var trStr = req.Tranche switch
        {
            TrancheType.T1Only => "الشطر الأول",
            TrancheType.T2Only => "الشطر الثاني",
            _                  => "الشطرين معاً"
        };
        var bdStr = req.Building == BuildingType.Talia ? "تعلية" : "أرضي";
        TxtLogicSummary.Text =
            $"⚙ المنطق النشط: {trStr} | {bdStr} | المبلغ: {logic.AmountNum} دج | النسب: {logic.Pct1Num} / {logic.Pct2Num}";
    }

    // ════════════════════ Tab change ════════════════════
    private void TabAnnexes_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateTemplateLabel();
    }
    private void UpdateTemplateLabel()
    {
        if (RunTemplateName == null) return;
        RunTemplateName.Text = TabAnnexes.SelectedIndex == 0
            ? "Annexe_05_Officiel.pdf"
            : "Annexe_06_Officiel.pdf";
    }

    // ════════════════════ Clear date buttons ════════════════════
    private void BtnClearDateDemande_Click(object sender, RoutedEventArgs e) => DpDateDemande.SelectedDate = null;
    private void BtnClearDateVisite_Click(object sender, RoutedEventArgs e)  => DpDateVisite.SelectedDate  = null;
    private void BtnClearDatePV_Click(object sender, RoutedEventArgs e)      => DpDatePV.SelectedDate      = null;

    // ════════════════════ Header buttons ════════════════════
    private void BtnSettings_Click(object sender, RoutedEventArgs e) => OpenSettings();

    private void OpenSettings()
    {
        var dlg = new SettingsWindow { Owner = this };
        if (dlg.ShowDialog() == true)
        {
            // الحفظ تم في النافذة نفسها — نُحدّث القوائم
            var s = SettingsService.Instance.Current;
            s.FirstRun = false;
            SettingsService.Instance.Save();
            RefreshSubdivDropdown();
            RecomputeLogic();
            ShowToast("✅ تم حفظ الإعدادات");
        }
    }

    private void BtnAbout_Click(object sender, RoutedEventArgs e)
    {
        var about = new AboutWindow { Owner = this };
        about.ShowDialog();
    }

    private void BtnBeneficiaires_Click(object sender, RoutedEventArgs e)
    {
        var win = new BeneficiairesWindow { Owner = this };
        if (win.ShowDialog() == true && win.SelectedBeneficiaire != null)
        {
            var b = win.SelectedBeneficiaire;
            TxtCode.Text        = b.Code;
            TxtNomPrenom.Text   = b.NomPrenom;
            TxtAdresse.Text     = b.Adresse;
            TxtCcp.Text         = b.NumeroCcp;
            DpDateDecision.SelectedDate = b.DateDecision;
            UpdateLast5();
            ShowToast($"📂 تم تحميل المستفيد: {b.NomPrenom}");
        }
    }

    // ════════════════════ Validation ════════════════════
    private bool ValidateShared()
    {
        if (string.IsNullOrWhiteSpace(TxtCode.Text)
         || string.IsNullOrWhiteSpace(TxtNomPrenom.Text)
         || string.IsNullOrWhiteSpace(TxtAdresse.Text)
         || string.IsNullOrWhiteSpace(TxtCcp.Text)
         || DpDateDecision.SelectedDate == null)
        {
            ShowToast("⚠ يرجى تعبئة جميع حقول بيانات المستفيد المشتركة (*)", ToastType.Error);
            return false;
        }
        return true;
    }

    private bool ValidateAnnexe06()
    {
        if (string.IsNullOrWhiteSpace(CbSubdiv.Text)
         || string.IsNullOrWhiteSpace(TxtPermisNum.Text)
         || DpDatePermis.SelectedDate == null)
        {
            ShowToast("⚠ حقول الملحق 06 الإجبارية مطلوبة (Subdivisionnaire/رقم وتاريخ رخصة البناء)", ToastType.Error);
            return false;
        }
        return true;
    }

    // ════════════════════ Show fields (diagnostic) ════════════════════
    private void BtnShowFields_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = GetTemplatePath();
            var names = PdfFiller.ListFields(path);
            _logs.Clear();
            _logs.Add($"📄 القالب: {Path.GetFileName(path)}");
            _logs.Add($"📋 عدد الحقول: {names.Count}");
            foreach (var n in names) _logs.Add("  • " + n);
            ShowToast("✅ تم عرض حقول القالب في سجل العمليات");
        }
        catch (Exception ex)
        {
            _logs.Add("❌ " + ex.Message);
            ShowToast("❌ خطأ: " + ex.Message, ToastType.Error);
        }
    }

    // ════════════════════ Generate ════════════════════
    private void BtnGenerate_Click(object sender, RoutedEventArgs e)
    {
        RecomputeLogic();
        if (!ValidateShared()) return;
        bool isA5 = TabAnnexes.SelectedIndex == 0;
        if (!isA5 && !ValidateAnnexe06()) return;

        var req = new AnnexeRequest
        {
            Beneficiaire     = BuildBeneficiaireFromForm(),
            AnnexeType       = isA5 ? AnnexeType.Annexe05 : AnnexeType.Annexe06,
            Tranche          = GetTranche(),
            Building         = GetBuilding(),
            DateDemande      = DpDateDemande.SelectedDate,
            DateVisite       = DpDateVisite.SelectedDate,
            DatePV           = DpDatePV.SelectedDate,
            Subdivisionnaire = CbSubdiv.Text?.Trim() ?? "",
            NumeroPermis     = TxtPermisNum.Text?.Trim() ?? "",
            DatePermis       = DpDatePermis.SelectedDate ?? DateTime.Today,
            Observation1     = TxtObs1.Text?.Trim() ?? "",
            Observation2     = TxtObs2.Text?.Trim() ?? "",
            ObservationComp  = TxtObsComp.Text?.Trim() ?? "",
        };

        try
        {
            var settings = SettingsService.Instance.Current;
            var logic    = LogicService.Compute(settings, req);
            var template = GetTemplatePath();

            _filler.Clear();
            _logs.Clear();
            _logs.Add($"🚀 بدء الحقن للملحق {(isA5 ? "05" : "06")}...");
            _logs.Add($"📌 المستفيد: {req.Beneficiaire.NomPrenom} ({req.Beneficiaire.Last5})");
            _logs.Add($"📌 الجهة: {settings.Wilaya} / {settings.Daira} / {settings.Commune}");

            byte[] pdfBytes = isA5
                ? _filler.FillAnnexe05(template, settings, req, logic)
                : _filler.FillAnnexe06(template, settings, req, logic);

            foreach (var l in _filler.Logs) _logs.Add(l);

            // حفظ مع SaveFileDialog
            var safeName = SafeFileName(req.Beneficiaire.NomPrenom);
            var dlg = new SaveFileDialog
            {
                Title = "حفظ ملف PDF",
                Filter = "PDF Files (*.pdf)|*.pdf",
                FileName = (isA5 ? "Annexe_05_" : "Annexe_06_") + safeName + "_" + TrancheSuffix(req.Tranche) + ".pdf"
            };
            if (dlg.ShowDialog() == true)
            {
                File.WriteAllBytes(dlg.FileName, pdfBytes);
                _logs.Add("🎉 تم حفظ الملف بنجاح: " + dlg.FileName);
                ShowToast("✅ تم حفظ ملف PDF بنجاح");

                var open = MessageBox.Show("هل تريد فتح الملف الآن؟", "تم",
                    MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (open == MessageBoxResult.Yes)
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    { FileName = dlg.FileName, UseShellExecute = true });
            }
        }
        catch (Exception ex)
        {
            _logs.Add("❌ خطأ: " + ex.Message);
            ShowToast("❌ خطأ: " + ex.Message, ToastType.Error);
        }
    }

    private static string SafeFileName(string s)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var clean = new string(s.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return clean.Length > 40 ? clean[..40] : clean;
    }

    private static string TrancheSuffix(TrancheType t) => t switch
    {
        TrancheType.T1Only => "S1",
        TrancheType.T2Only => "S2",
        _                  => "S1S2"
    };

    private string GetTemplatePath()
    {
        var fileName = TabAnnexes.SelectedIndex == 0
            ? "Annexe_05_Officiel.pdf"
            : "Annexe_06_Officiel.pdf";
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "Templates", fileName);
        if (!File.Exists(path))
            throw new FileNotFoundException(
                $"القالب الرسمي غير موجود: {path}\n" +
                $"يرجى التأكد من نسخ ملف {fileName} داخل مجلد Assets/Templates.");
        return path;
    }
}

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
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

    // ─── Chargement initial ───────────────────────────────────────────────
    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (DpDateDecision != null) DpDateDecision.SelectedDate = DateTime.Today;
        if (DpDatePermis   != null) DpDatePermis.SelectedDate   = DateTime.Today;
        if (DpDateDemande  != null) DpDateDemande.SelectedDate  = null;
        if (DpDateVisite   != null) DpDateVisite.SelectedDate   = null;
        if (DpDatePV       != null) DpDatePV.SelectedDate       = null;

        RefreshSubdivDropdown();
        RefreshBeneficiairesDropdown();
        UpdateLast5();
        RecomputeLogic();
        UpdateAnnexeSections();
        RefreshArchive();

        if (SettingsService.Instance.Current.FirstRun)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                ShowToast("👋 مرحباً! يرجى ضبط الإعدادات العامة في أول تشغيل", ToastType.Warn);
                OpenSettings();
            }), DispatcherPriority.Background);
        }
    }

    // ─── Toast ───────────────────────────────────────────────────────────
    private enum ToastType { Success, Warn, Error }
    private DispatcherTimer? _toastTimer;

    private void ShowToast(string msg, ToastType type = ToastType.Success)
    {
        if (TxtToast == null || Toast == null) return;
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
        _toastTimer.Tick += (_, _) => { if (Toast != null) Toast.Visibility = Visibility.Collapsed; _toastTimer!.Stop(); };
        _toastTimer.Start();
    }

    // ─── Sélection du type d'annexe ──────────────────────────────────────
    private void AnnexeType_Changed(object sender, RoutedEventArgs e) => UpdateAnnexeSections();

    private void UpdateAnnexeSections()
    {
        bool isA5 = RbAnnexe5?.IsChecked == true;
        if (PanelAnnexe5Fields != null) PanelAnnexe5Fields.Visibility = isA5 ? Visibility.Visible  : Visibility.Collapsed;
        if (PanelAnnexe6Fields != null) PanelAnnexe6Fields.Visibility = isA5 ? Visibility.Collapsed : Visibility.Visible;
        RecomputeLogic();
    }

    // ─── Logique financière ───────────────────────────────────────────────
    private void Logic_Changed(object sender, RoutedEventArgs e) => RecomputeLogic();

    private TrancheType GetTranche()
        => RbT1?.IsChecked  == true ? TrancheType.T1Only
        : RbT2?.IsChecked   == true ? TrancheType.T2Only
        : TrancheType.Both;

    private BuildingType GetBuilding()
        => RbTalia?.IsChecked == true ? BuildingType.Talia : BuildingType.Ardhi;

    private void RecomputeLogic()
    {
        if (TxtLogicSummary == null) return;
        var req = new AnnexeRequest
        {
            Beneficiaire = BuildBeneficiaireFromForm(),
            Tranche      = GetTranche(),
            Building     = GetBuilding(),
        };
        var logic  = LogicService.Compute(SettingsService.Instance.Current, req);
        var trStr  = req.Tranche switch
        {
            TrancheType.T1Only => "الشطر الأول",
            TrancheType.T2Only => "الشطر الثاني",
            _                  => "الشطرين معاً"
        };
        var bdStr  = req.Building == BuildingType.Talia ? "تعلية" : "أرضي";
        var annexe = RbAnnexe5?.IsChecked == true ? "Annexe 5" : "Annexe 6";
        TxtLogicSummary.Text =
            $"⚙ النشط: {annexe} | {trStr} | {bdStr} | المبلغ: {logic.AmountNum} دج | {logic.Pct1Num} / {logic.Pct2Num}";
    }

    // ─── Champ Code ──────────────────────────────────────────────────────
    private void TxtCode_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateLast5();
        RecomputeLogic();
    }

    private void UpdateLast5()
    {
        if (TxtLast5 == null) return;
        var code = (TxtCode?.Text ?? "").Trim();
        TxtLast5.Text = code.Length >= 5 ? code[^5..] : code;
    }

    // ─── Recherche par 5 chiffres ─────────────────────────────────────────
    private void BtnSearch_Click(object sender, RoutedEventArgs e) => DoSearch();
    private void TxtSearchCode_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) DoSearch();
    }

    private void DoSearch()
    {
        var q = TxtSearchCode?.Text?.Trim() ?? "";
        if (q.Length == 0) { ShowToast("أدخل آخر 5 أرقام من الكود", ToastType.Warn); return; }

        var results = BeneficiairesRepository.Instance.SearchByLast5(q);

        if (results.Count == 0)
        {
            ShowToast("⚠ لم يتم العثور على مستفيد بهذا الكود", ToastType.Warn);
            return;
        }

        // تحديث القائمة المنسدلة بالنتائج
        CbExistingBeneficiaires.ItemsSource = results;

        if (results.Count == 1)
        {
            CbExistingBeneficiaires.SelectedItem = results[0];
            LoadBeneficiaire(results[0]);
            ShowToast($"✅ تم تحميل: {results[0].NomPrenom}");
        }
        else
        {
            CbExistingBeneficiaires.IsDropDownOpen = true;
            ShowToast($"تم العثور على {results.Count} مستفيدين — اختر من القائمة", ToastType.Warn);
        }
    }

    // ─── ComboBox مستفيدين ────────────────────────────────────────────────
    private void RefreshBeneficiairesDropdown()
    {
        if (CbExistingBeneficiaires == null) return;
        var prev = CbExistingBeneficiaires.SelectedItem as Beneficiaire;
        var list = new ObservableCollection<Beneficiaire>(BeneficiairesRepository.Instance.All);
        CbExistingBeneficiaires.ItemsSource = list;
        if (prev != null)
            CbExistingBeneficiaires.SelectedItem = list.FirstOrDefault(x => x.Id == prev.Id);
    }

    private void CbExistingBeneficiaires_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CbExistingBeneficiaires?.SelectedItem is Beneficiaire b)
            LoadBeneficiaire(b);
    }

    private void LoadBeneficiaire(Beneficiaire b)
    {
        if (TxtCode        != null) TxtCode.Text               = b.Code;
        if (TxtNomPrenom   != null) TxtNomPrenom.Text          = b.NomPrenom;
        if (TxtAdresse     != null) TxtAdresse.Text            = b.Adresse;
        if (TxtCcp         != null) TxtCcp.Text                = b.NumeroCcp;
        if (DpDateDecision != null) DpDateDecision.SelectedDate = b.DateDecision;
        UpdateLast5();
    }

    // ─── Sauvegarde / suppression de bénéficiaire ────────────────────────
    private void BtnSaveBeneficiaire_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateShared()) return;
        var b = BuildBeneficiaireFromForm();
        var existing = BeneficiairesRepository.Instance.All
            .FirstOrDefault(x => string.Equals(x.Code, b.Code, StringComparison.OrdinalIgnoreCase));
        if (existing != null) b.Id = existing.Id;
        BeneficiairesRepository.Instance.Save(b);
        RefreshBeneficiairesDropdown();
        ShowToast(existing != null ? "✅ تم تحديث بيانات المستفيد" : "✅ تمت إضافة مستفيد جديد");
    }

    private void BtnDeleteBeneficiaire_Click(object sender, RoutedEventArgs e)
    {
        if (CbExistingBeneficiaires?.SelectedItem is not Beneficiaire b)
        {
            ShowToast("⚠ اختر مستفيداً من القائمة أولاً", ToastType.Warn);
            return;
        }
        var ok = MessageBox.Show($"هل تريد حذف المستفيد:\n{b.NomPrenom}؟",
            "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (ok != MessageBoxResult.Yes) return;
        BeneficiairesRepository.Instance.Delete(b.Id);
        RefreshBeneficiairesDropdown();
        ShowToast("🗑 تم الحذف", ToastType.Warn);
    }

    private Beneficiaire BuildBeneficiaireFromForm() => new()
    {
        Code         = (TxtCode?.Text       ?? "").Trim(),
        NomPrenom    = (TxtNomPrenom?.Text  ?? "").Trim(),
        Adresse      = (TxtAdresse?.Text    ?? "").Trim(),
        NumeroCcp    = (TxtCcp?.Text        ?? "").Trim(),
        DateDecision = DpDateDecision?.SelectedDate ?? DateTime.Today,
    };

    // ─── Subdivisionnaire ────────────────────────────────────────────────
    private void RefreshSubdivDropdown()
    {
        if (CbSubdiv == null) return;
        var prev = CbSubdiv.Text;
        CbSubdiv.ItemsSource = null;
        CbSubdiv.ItemsSource = SettingsService.Instance.Current.Subdivisionnaires;
        if (!string.IsNullOrEmpty(prev) && SettingsService.Instance.Current.Subdivisionnaires.Contains(prev))
            CbSubdiv.Text = prev;
        else if (SettingsService.Instance.Current.Subdivisionnaires.Count > 0)
            CbSubdiv.Text = SettingsService.Instance.Current.Subdivisionnaires[0];
    }

    // ─── Boutons de date vides ────────────────────────────────────────────
    private void BtnClearDateDemande_Click(object sender, RoutedEventArgs e) { if (DpDateDemande != null) DpDateDemande.SelectedDate = null; }
    private void BtnClearDateVisite_Click (object sender, RoutedEventArgs e) { if (DpDateVisite  != null) DpDateVisite.SelectedDate  = null; }
    private void BtnClearDatePV_Click     (object sender, RoutedEventArgs e) { if (DpDatePV      != null) DpDatePV.SelectedDate      = null; }

    // ─── Paramètres / À propos ────────────────────────────────────────────
    private void BtnSettings_Click(object sender, RoutedEventArgs e) => OpenSettings();
    private void BtnAbout_Click   (object sender, RoutedEventArgs e) => new AboutWindow { Owner = this }.ShowDialog();

    private void OpenSettings()
    {
        var dlg = new SettingsWindow { Owner = this };
        if (dlg.ShowDialog() == true)
        {
            SettingsService.Instance.Current.FirstRun = false;
            SettingsService.Instance.Save();
            RefreshSubdivDropdown();
            RecomputeLogic();
            ShowToast("✅ تم حفظ الإعدادات");
        }
    }

    // ─── Validation ──────────────────────────────────────────────────────
    private bool ValidateShared()
    {
        if (string.IsNullOrWhiteSpace(TxtCode?.Text)      ||
            string.IsNullOrWhiteSpace(TxtNomPrenom?.Text) ||
            string.IsNullOrWhiteSpace(TxtAdresse?.Text)   ||
            string.IsNullOrWhiteSpace(TxtCcp?.Text)       ||
            DpDateDecision?.SelectedDate == null)
        {
            ShowToast("⚠ يرجى تعبئة جميع الحقول الإجبارية (*)", ToastType.Error);
            return false;
        }
        return true;
    }

    private bool ValidateAnnexe06()
    {
        if (string.IsNullOrWhiteSpace(CbSubdiv?.Text) ||
            string.IsNullOrWhiteSpace(TxtPermisNum?.Text) ||
            DpDatePermis?.SelectedDate == null)
        {
            ShowToast("⚠ حقول Annexe 06 الإجبارية ناقصة (Subdivisionnaire / رخصة البناء)", ToastType.Error);
            return false;
        }
        return true;
    }

    // ─── Génération + archivage + aperçu ─────────────────────────────────
    private void BtnGenerate_Click(object sender, RoutedEventArgs e)
    {
        RecomputeLogic();
        if (!ValidateShared()) return;

        bool isA5 = RbAnnexe5?.IsChecked == true;
        if (!isA5 && !ValidateAnnexe06()) return;

        var req = new AnnexeRequest
        {
            Beneficiaire     = BuildBeneficiaireFromForm(),
            AnnexeType       = isA5 ? AnnexeType.Annexe05 : AnnexeType.Annexe06,
            Tranche          = GetTranche(),
            Building         = GetBuilding(),
            DateDemande      = DpDateDemande?.SelectedDate,
            DateVisite       = DpDateVisite?.SelectedDate,
            DatePV           = DpDatePV?.SelectedDate,
            Subdivisionnaire = CbSubdiv?.Text?.Trim()      ?? "",
            NumeroPermis     = TxtPermisNum?.Text?.Trim()  ?? "",
            DatePermis       = DpDatePermis?.SelectedDate  ?? DateTime.Today,
            Observation1     = TxtObs1?.Text?.Trim()       ?? "",
            Observation2     = TxtObs2?.Text?.Trim()       ?? "",
            ObservationComp  = TxtObsComp?.Text?.Trim()    ?? "",
        };

        try
        {
            var settings = SettingsService.Instance.Current;
            var logic    = LogicService.Compute(settings, req);
            var template = GetTemplatePath();

            _filler.Clear();
            _logs.Clear();
            _logs.Add($"🚀 بدء الحقن — الملحق {(isA5 ? "05" : "06")}");
            _logs.Add($"📌 {req.Beneficiaire.NomPrenom}  ({req.Beneficiaire.Last5})");

            byte[] pdfBytes = isA5
                ? _filler.FillAnnexe05(template, settings, req, logic)
                : _filler.FillAnnexe06(template, settings, req, logic);

            foreach (var l in _filler.Logs) _logs.Add(l);

            // ── أرشفة تلقائية ──
            var archivePath = ArchiveService.Instance.Save(req, pdfBytes);
            _logs.Add($"📁 الأرشيف: {archivePath}");
            RefreshArchive();

            // ── فتح للمعاينة ──
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName        = archivePath,
                UseShellExecute = true
            });

            ShowToast($"✅ تم الإنشاء والحفظ في الأرشيف — {Path.GetFileName(archivePath)}");
        }
        catch (Exception ex)
        {
            _logs.Add("❌ " + ex.Message);
            ShowToast("❌ خطأ: " + ex.Message, ToastType.Error);
        }
    }

    private string GetTemplatePath()
    {
        bool isA5    = RbAnnexe5?.IsChecked == true;
        var fileName = isA5 ? "Annexe_05_Officiel.pdf" : "Annexe_06_Officiel.pdf";
        var path     = Path.Combine(AppContext.BaseDirectory, "Assets", "Templates", fileName);
        if (!File.Exists(path))
            throw new FileNotFoundException($"القالب غير موجود: {path}");
        return path;
    }

    // ─── الأرشيف ─────────────────────────────────────────────────────────
    private void RefreshArchive()
    {
        var list = ArchiveService.Instance.GetAll();
        if (DgArchives != null) DgArchives.ItemsSource = list;
        if (RunArchiveCount != null) RunArchiveCount.Text = list.Count.ToString();
    }

    private void BtnRefreshArchive_Click(object sender, RoutedEventArgs e) => RefreshArchive();

    private void BtnDeleteArchive_Click(object sender, RoutedEventArgs e)
    {
        if (DgArchives?.SelectedItem is not ArchiveEntry entry)
        {
            ShowToast("⚠ اختر سطراً من الأرشيف أولاً", ToastType.Warn);
            return;
        }
        var ok = MessageBox.Show($"حذف هذه النسخة من الأرشيف؟\n{entry.FileName}",
            "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (ok != MessageBoxResult.Yes) return;
        ArchiveService.Instance.Delete(entry.Id, deleteFile: true);
        RefreshArchive();
        ShowToast("🗑 تم الحذف من الأرشيف", ToastType.Warn);
    }

    private void DgArchives_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DgArchives?.SelectedItem is ArchiveEntry entry)
        {
            if (!File.Exists(entry.FilePath))
            {
                ShowToast("⚠ الملف غير موجود على القرص", ToastType.Error);
                return;
            }
            ArchiveService.Open(entry);
        }
    }

    private void BtnOpenArchiveFolder_Click(object sender, RoutedEventArgs e)
    {
        var folder = ArchiveService.Instance.ArchiveFolder;
        if (Directory.Exists(folder))
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName        = folder,
                UseShellExecute = true
            });
    }
}

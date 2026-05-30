using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HabitatRural.Models;
using HabitatRural.Services;

namespace HabitatRural.Views {
    public partial class MainWindow : Window {
        private ObservableCollection<string> _logs = new();
        private PdfFiller _filler = new();
        private bool _isAnnexe6 = false;
        private byte[] _currentPdfBytes = null;
        private string _currentTempPdf = null;

        public MainWindow() {
            InitializeComponent();
            LogList.ItemsSource = _logs;
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            DpDateDecision.SelectedDate = DateTime.Today;
            DpDatePermis.SelectedDate = DateTime.Today;
            AnnexeType_Click(BtnAnnexe5, null);
        }

        private void AnnexeType_Click(object sender, RoutedEventArgs e) {
            if (sender == BtnAnnexe5) {
                _isAnnexe6 = false;
                BtnAnnexe5.Background = (System.Windows.Media.Brush)FindResource("PrimaryBrush");
                BtnAnnexe5.Foreground = System.Windows.Media.Brushes.White;
                BtnAnnexe6.Background = System.Windows.Media.Brushes.LightGray;
                BtnAnnexe6.Foreground = System.Windows.Media.Brushes.Black;
                Annexe6Panel.Visibility = Visibility.Collapsed;
            } else {
                _isAnnexe6 = true;
                BtnAnnexe6.Background = (System.Windows.Media.Brush)FindResource("PrimaryBrush");
                BtnAnnexe6.Foreground = System.Windows.Media.Brushes.White;
                BtnAnnexe5.Background = System.Windows.Media.Brushes.LightGray;
                BtnAnnexe5.Foreground = System.Windows.Media.Brushes.Black;
                Annexe6Panel.Visibility = Visibility.Visible;
            }
        }

        private void TxtCode_TextChanged(object sender, TextChangedEventArgs e) { }

        private void BtnSearch_Click(object sender, RoutedEventArgs e) {
            string input = TxtCode.Text.Trim();
            if (input.Length < 5) {
                ShowToast("الرجاء إدخال 5 أرقام على الأقل", ToastType.Warn);
                return;
            }
            string last5 = input.Length > 5 ? input.Substring(input.Length - 5) : input;
            var result = DatabaseService.Instance.SearchByLast5(last5);
            if (result != null) {
                TxtNomPrenom.Text = result.NomPrenom;
                TxtAdresse.Text = result.Adresse;
                TxtCcp.Text = result.NumeroCcp;
                DpDateDecision.SelectedDate = result.DateDecision;
                TxtCode.Text = result.Code;
                ShowToast($"تم العثور: {result.NomPrenom}", ToastType.Success);
            } else {
                ShowToast("لم يتم العثور على مستفيد بهذه الأرقام", ToastType.Warn);
            }
        }

        private void BtnPreview_Click(object sender, RoutedEventArgs e) {
            if (_currentPdfBytes == null) GeneratePdfFile(false);
            if (_currentPdfBytes != null && File.Exists(_currentTempPdf))
                new PdfPreviewWindow(_currentTempPdf).ShowDialog();
        }

        private void BtnGenerate_Click(object sender, RoutedEventArgs e) => GeneratePdfFile(true);

        private void GeneratePdfFile(bool saveAndArchive) {
            if (!ValidateShared()) return;
            var req = BuildRequest();
            try {
                var settings = SettingsService.Instance.Current;
                var logic = LogicService.Compute(settings, req);
                string template = GetTemplatePath();

                _filler.Clear();
                _logs.Clear();
                _logs.Add($"بدء الحقن للملحق {(_isAnnexe6 ? "06" : "05")}...");
                _logs.Add($"المستفيد: {req.Beneficiaire.NomPrenom}");

                _currentPdfBytes = _isAnnexe6
                    ? _filler.FillAnnexe06(template, settings, req, logic)
                    : _filler.FillAnnexe05(template, settings, req, logic);
                foreach (var l in _filler.Logs) _logs.Add(l);

                if (saveAndArchive) {
                    string suffix = "T1T2";
                    string fileName = $"{SanitizeFileName(req.Beneficiaire.NomPrenom)}_{(_isAnnexe6 ? "Annexe6" : "Annexe5")}_{suffix}.pdf";
                    string archiveFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "HabitatRural_Archives");
                    Directory.CreateDirectory(archiveFolder);
                    string archivePath = Path.Combine(archiveFolder, fileName);
                    File.WriteAllBytes(archivePath, _currentPdfBytes);
                    _logs.Add($"تم أرشفة الملحق في: {archivePath}");
                    ShowToast("تم حفظ PDF في مجلد الأرشيف", ToastType.Success);
                    DatabaseService.Instance.SaveBeneficiaire(req.Beneficiaire);
                } else {
                    _currentTempPdf = Path.GetTempFileName() + ".pdf";
                    File.WriteAllBytes(_currentTempPdf, _currentPdfBytes);
                }
            } catch (Exception ex) {
                _logs.Add("خطأ: " + ex.Message);
                ShowToast("خطأ: " + ex.Message, ToastType.Error);
            }
        }

        private AnnexeRequest BuildRequest() {
            return new AnnexeRequest {
                Beneficiaire = BuildBeneficiaireFromForm(),
                AnnexeType = _isAnnexe6 ? AnnexeType.Annexe06 : AnnexeType.Annexe05,
                Tranche = TrancheType.Both,
                Building = BuildingType.Talia,
                Subdivisionnaire = "AMRANI AHMED",
                NumeroPermis = TxtPermisNum.Text.Trim(),
                DatePermis = DpDatePermis.SelectedDate ?? DateTime.Today
            };
        }

        private Beneficiaire BuildBeneficiaireFromForm() {
            return new Beneficiaire {
                Code = TxtCode.Text.Trim(),
                NomPrenom = TxtNomPrenom.Text.Trim(),
                Adresse = TxtAdresse.Text.Trim(),
                NumeroCcp = TxtCcp.Text.Trim(),
                DateDecision = DpDateDecision.SelectedDate ?? DateTime.Today
            };
        }

        private bool ValidateShared() {
            if (string.IsNullOrWhiteSpace(TxtCode.Text) || string.IsNullOrWhiteSpace(TxtNomPrenom.Text) ||
                string.IsNullOrWhiteSpace(TxtAdresse.Text) || string.IsNullOrWhiteSpace(TxtCcp.Text) ||
                DpDateDecision.SelectedDate == null) {
                ShowToast("يرجى تعبئة جميع الحقول الإجبارية", ToastType.Error);
                return false;
            }
            if (_isAnnexe6 && string.IsNullOrWhiteSpace(TxtPermisNum.Text)) {
                ShowToast("رقم رخصة البناء مطلوب", ToastType.Error);
                return false;
            }
            return true;
        }

        private string GetTemplatePath() {
            string fileName = _isAnnexe6 ? "Annexe_06_Officiel.pdf" : "Annexe_05_Officiel.pdf";
            string path = Path.Combine(AppContext.BaseDirectory, "Assets", "Templates", fileName);
            if (!File.Exists(path))
                throw new FileNotFoundException($"القالب غير موجود: {path}");
            return path;
        }

        private string SanitizeFileName(string name) {
            var invalid = Path.GetInvalidFileNameChars();
            var clean = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
            return clean.Length > 50 ? clean.Substring(0,50) : clean;
        }

        private enum ToastType { Success, Warn, Error }
        private void ShowToast(string msg, ToastType type = ToastType.Success) {
            MessageBoxImage img = type == ToastType.Error ? MessageBoxImage.Error : type == ToastType.Warn ? MessageBoxImage.Warning : MessageBoxImage.Information;
            MessageBox.Show(msg, type == ToastType.Error ? "خطأ" : "تنبيه", MessageBoxButton.OK, img);
        }
    }
}
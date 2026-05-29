using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HabitatRural.Models;
using HabitatRural.Services;

namespace HabitatRural.Views;

public partial class SettingsWindow : Window
{
    // wrapper observable لتمكين التعديل المباشر داخل ItemsControl
    public class StringBox
    {
        public string Value { get; set; } = "";
    }

    private readonly ObservableCollection<StringBox> _subdivs = new();

    public SettingsWindow()
    {
        InitializeComponent();
        Load();
    }

    private void Load()
    {
        var s = SettingsService.Instance.Current;
        TxtWilaya.Text       = s.Wilaya;
        TxtDaira.Text        = s.Daira;
        TxtCommune.Text      = s.Commune;
        TxtTotalAmount.Text  = s.TotalAmount.ToString("0");
        TxtPctT1.Text        = s.PctT1.ToString();
        TxtPctT2.Text        = s.PctT2.ToString();

        _subdivs.Clear();
        foreach (var n in s.Subdivisionnaires) _subdivs.Add(new StringBox { Value = n });
        SubdivList.ItemsSource = _subdivs;
    }

    private void BtnAddSubdiv_Click(object sender, RoutedEventArgs e)
    {
        _subdivs.Add(new StringBox { Value = "" });
    }

    private void BtnRemoveSubdiv_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is StringBox sb)
            _subdivs.Remove(sb);
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(TxtPctT1.Text, out var pct1) || pct1 < 0 || pct1 > 100)
        {
            MessageBox.Show("نسبة الشطر الأول غير صالحة.", "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        if (!int.TryParse(TxtPctT2.Text, out var pct2) || pct2 < 0 || pct2 > 100)
        {
            MessageBox.Show("نسبة الشطر الثاني غير صالحة.", "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        if (pct1 + pct2 != 100)
        {
            MessageBox.Show(
                $"مجموع نسب الأشطر يجب أن يساوي 100% (حالياً {pct1 + pct2}%).",
                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        if (!decimal.TryParse(TxtTotalAmount.Text, out var total) || total <= 0)
        {
            MessageBox.Show("المبلغ الإجمالي غير صالح.", "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var names = _subdivs
            .Select(x => x.Value?.Trim() ?? "")
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
        if (names.Count == 0) names.Add("AMRANI AHMED");

        var s = new AppSettings
        {
            Wilaya             = string.IsNullOrWhiteSpace(TxtWilaya.Text)  ? "CHLEF" : TxtWilaya.Text.Trim(),
            Daira              = string.IsNullOrWhiteSpace(TxtDaira.Text)   ? "CHLEF" : TxtDaira.Text.Trim(),
            Commune            = string.IsNullOrWhiteSpace(TxtCommune.Text) ? ""      : TxtCommune.Text.Trim(),
            TotalAmount        = total,
            PctT1              = pct1,
            PctT2              = pct2,
            Subdivisionnaires  = names,
            FirstRun           = false,
        };
        SettingsService.Instance.Replace(s);
        DialogResult = true;
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

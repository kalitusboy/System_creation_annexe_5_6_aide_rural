using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HabitatRural.Models;
using HabitatRural.Services;

namespace HabitatRural.Views;

public partial class BeneficiairesWindow : Window
{
    // المستفيد الذي اختاره المستخدم (null إذا أغلق بدون اختيار)
    public Beneficiaire? SelectedBeneficiaire { get; private set; }

    public BeneficiairesWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => RefreshGrid();
    }

    // ── تحديث الشبكة ──────────────────────────────────────────────────────
    private void RefreshGrid(string? query = null)
    {
        var list = BeneficiairesRepository.Instance.Search(query);
        DgBeneficiaires.ItemsSource = list;
        var total = BeneficiairesRepository.Instance.GetAll().Count;
        TxtCount.Text = $"إجمالي المستفيدين: {total}  |  نتائج البحث: {list.Count}";
    }

    // ── بحث ───────────────────────────────────────────────────────────────
    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        => RefreshGrid(TxtSearch.Text.Trim());

    // ── اختيار ─────────────────────────────────────────────────────────────
    private void Dg_DoubleClick(object sender, MouseButtonEventArgs e)
        => SelectCurrent();

    private void BtnSelect_Click(object sender, RoutedEventArgs e)
        => SelectCurrent();

    private void SelectCurrent()
    {
        if (DgBeneficiaires.SelectedItem is Beneficiaire b)
        {
            SelectedBeneficiaire = b;
            DialogResult = true;
            Close();
        }
        else
        {
            MessageBox.Show("اختر مستفيداً أولاً من القائمة.",
                "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    // ── تعديل ─────────────────────────────────────────────────────────────
    private void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        if (DgBeneficiaires.SelectedItem is not Beneficiaire b)
        {
            MessageBox.Show("اختر مستفيداً أولاً.", "تنبيه",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var dlg = new BeneficiaireEditWindow(b) { Owner = this };
        if (dlg.ShowDialog() == true)
            RefreshGrid(TxtSearch.Text.Trim());
    }

    // ── حذف ───────────────────────────────────────────────────────────────
    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (DgBeneficiaires.SelectedItem is not Beneficiaire b) return;
        var ok = MessageBox.Show(
            $"هل تريد حذف المستفيد:\n{b.NomPrenom}؟",
            "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (ok != MessageBoxResult.Yes) return;
        BeneficiairesRepository.Instance.Delete(b.Id);
        RefreshGrid(TxtSearch.Text.Trim());
    }
}

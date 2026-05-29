using System;
using System.Windows;
using HabitatRural.Models;
using HabitatRural.Services;

namespace HabitatRural.Views;

public partial class BeneficiaireEditWindow : Window
{
    private readonly Beneficiaire _b;

    public BeneficiaireEditWindow(Beneficiaire? existing = null)
    {
        InitializeComponent();
        _b = existing != null
            ? new Beneficiaire
              {
                  Id           = existing.Id,
                  Code         = existing.Code,
                  NomPrenom    = existing.NomPrenom,
                  Adresse      = existing.Adresse,
                  DateDecision = existing.DateDecision,
                  NumeroCcp    = existing.NumeroCcp,
              }
            : new Beneficiaire();

        Loaded += (_, _) => LoadToForm();
    }

    private void LoadToForm()
    {
        TxtCode.Text   = _b.Code;
        TxtNom.Text    = _b.NomPrenom;
        TxtAdresse.Text = _b.Adresse;
        TxtCcp.Text    = _b.NumeroCcp;
        DpDate.SelectedDate = _b.DateDecision == default ? DateTime.Today : _b.DateDecision;
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtCode.Text) ||
            string.IsNullOrWhiteSpace(TxtNom.Text)  ||
            DpDate.SelectedDate == null)
        {
            MessageBox.Show("يرجى ملء الحقول الإجبارية (*)",
                "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        _b.Code         = TxtCode.Text.Trim();
        _b.NomPrenom    = TxtNom.Text.Trim();
        _b.Adresse      = TxtAdresse.Text.Trim();
        _b.NumeroCcp    = TxtCcp.Text.Trim();
        _b.DateDecision = DpDate.SelectedDate.Value;
        BeneficiairesRepository.Instance.Save(_b);
        DialogResult = true;
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e) => Close();
}

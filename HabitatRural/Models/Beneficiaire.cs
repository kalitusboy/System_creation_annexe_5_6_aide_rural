using System;

namespace HabitatRural.Models;

/// <summary>
/// المستفيد - يطابق "البيانات المشتركة" في واجهة HTML
/// </summary>
public class Beneficiaire
{
    public int      Id             { get; set; }
    public string   Code           { get; set; } = "";   // 17 رقم
    public string   NomPrenom      { get; set; } = "";
    public string   Adresse        { get; set; } = "";   // Fraction
    public DateTime DateDecision   { get; set; } = DateTime.Today;
    public string   NumeroCcp      { get; set; } = "";
    public DateTime CreatedAt      { get; set; } = DateTime.Now;
    public DateTime UpdatedAt      { get; set; } = DateTime.Now;

    public string Last5 => string.IsNullOrEmpty(Code) || Code.Length < 5
        ? Code
        : Code[^5..];

    public override string ToString() => $"{NomPrenom} ({Last5})";
}

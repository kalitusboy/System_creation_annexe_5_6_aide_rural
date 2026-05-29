using System;

namespace HabitatRural.Models;

public class Beneficiaire
{
    public int      Id             { get; set; }
    public string   Code           { get; set; } = "";
    public string   NomPrenom      { get; set; } = "";
    public string   Adresse        { get; set; } = "";
    public DateTime DateDecision   { get; set; } = DateTime.Today;
    public string   NumeroCcp      { get; set; } = "";
    public DateTime CreatedAt      { get; set; } = DateTime.Now;
    public DateTime UpdatedAt      { get; set; } = DateTime.Now;

    public string Last5 => string.IsNullOrEmpty(Code) || Code.Length < 5 ? Code : Code[^5..];
    public override string ToString() => $"{NomPrenom} ({Last5})";
}

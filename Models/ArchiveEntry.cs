using System;

namespace HabitatRural.Models;

public class ArchiveEntry
{
    public int      Id             { get; set; }
    public int      BeneficiaireId { get; set; }
    public string   NomPrenom      { get; set; } = "";
    public string   AnnexeType     { get; set; } = "";
    public string   Tranche        { get; set; } = "";
    public string   Building       { get; set; } = "";
    public string   FilePath       { get; set; } = "";
    public string   FileName       { get; set; } = "";
    public DateTime GeneratedAt    { get; set; } = DateTime.Now;

    public string GeneratedAtStr => GeneratedAt.ToString("dd/MM/yyyy HH:mm");
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HabitatRural.Models;
using Microsoft.Data.Sqlite;

namespace HabitatRural.Services;

/// <summary>
/// خدمة أرشفة الملاحق: تحفظ نسخة PDF تلقائياً وتسجّل المعلومات في SQLite.
/// </summary>
public class ArchiveService
{
    private static readonly Lazy<ArchiveService> _lazy = new(() => new ArchiveService());
    public static ArchiveService Instance => _lazy.Value;

    public string ArchiveFolder { get; }
    private readonly string _cs;

    private ArchiveService()
    {
        _cs = DatabaseService.Instance.ConnectionString;
        ArchiveFolder = Path.Combine(SettingsService.Instance.SettingsFolder, "Archives");
        Directory.CreateDirectory(ArchiveFolder);
    }

    // ─── Sauvegarder un PDF généré ─────────────────────────────────────────
    /// <summary>
    /// Enregistre les octets PDF dans le dossier Archives et insère une entrée en base.
    /// Retourne le chemin complet du fichier sauvegardé.
    /// </summary>
    public string Save(AnnexeRequest req, byte[] pdfBytes)
    {
        var safeName  = SafeFileName(req.Beneficiaire.NomPrenom);
        var trancheStr = req.Tranche switch
        {
            TrancheType.T1Only => "T1",
            TrancheType.T2Only => "T2",
            _                  => "T1-T2"
        };
        var buildStr  = req.Building  == BuildingType.Talia    ? "Surelev" : "Ardhi";
        var annexeStr = req.AnnexeType == AnnexeType.Annexe05  ? "05" : "06";
        var stamp     = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        var fileName  = $"Annexe_{annexeStr}_{safeName}_{trancheStr}_{buildStr}_{stamp}.pdf";
        var filePath  = Path.Combine(ArchiveFolder, fileName);

        File.WriteAllBytes(filePath, pdfBytes);

        try
        {
            using var conn = new SqliteConnection(_cs);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Archives
                    (BeneficiaireId, NomPrenom, AnnexeType, Tranche, Building, FilePath, FileName, GeneratedAt)
                VALUES (0, $nom, $ann, $tr, $bd, $fp, $fn, $ga)";
            cmd.Parameters.AddWithValue("$nom", req.Beneficiaire.NomPrenom);
            cmd.Parameters.AddWithValue("$ann", $"Annexe {annexeStr}");
            cmd.Parameters.AddWithValue("$tr",  trancheStr);
            cmd.Parameters.AddWithValue("$bd",  req.Building == BuildingType.Talia ? "تعلية" : "أرضي");
            cmd.Parameters.AddWithValue("$fp",  filePath);
            cmd.Parameters.AddWithValue("$fn",  fileName);
            cmd.Parameters.AddWithValue("$ga",  DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.ExecuteNonQuery();
        }
        catch { /* ignorer erreurs DB */ }

        return filePath;
    }

    // ─── Lister les archives (les 300 dernières) ───────────────────────────
    public List<ArchiveEntry> GetAll()
    {
        var list = new List<ArchiveEntry>();
        try
        {
            using var conn = new SqliteConnection(_cs);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
                "SELECT Id,BeneficiaireId,NomPrenom,AnnexeType,Tranche,Building,FilePath,FileName,GeneratedAt " +
                "FROM Archives ORDER BY Id DESC LIMIT 300";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new ArchiveEntry
                {
                    Id             = r.GetInt32(0),
                    BeneficiaireId = r.GetInt32(1),
                    NomPrenom      = r.GetString(2),
                    AnnexeType     = r.GetString(3),
                    Tranche        = r.GetString(4),
                    Building       = r.GetString(5),
                    FilePath       = r.GetString(6),
                    FileName       = r.GetString(7),
                    GeneratedAt    = DateTime.TryParse(r.GetString(8), out var dt) ? dt : DateTime.Now,
                });
            }
        }
        catch { }
        return list;
    }

    // ─── Supprimer une entrée (+ fichier si demandé) ──────────────────────
    public void Delete(int id, bool deleteFile = true)
    {
        try
        {
            string? filePath = null;
            using var conn = new SqliteConnection(_cs);
            conn.Open();

            using var sel = conn.CreateCommand();
            sel.CommandText = "SELECT FilePath FROM Archives WHERE Id=$id";
            sel.Parameters.AddWithValue("$id", id);
            filePath = sel.ExecuteScalar() as string;

            using var del = conn.CreateCommand();
            del.CommandText = "DELETE FROM Archives WHERE Id=$id";
            del.Parameters.AddWithValue("$id", id);
            del.ExecuteNonQuery();

            if (deleteFile && filePath != null && File.Exists(filePath))
                File.Delete(filePath);
        }
        catch { }
    }

    // ─── Ouvrir un fichier archivé ─────────────────────────────────────────
    public static void Open(ArchiveEntry entry)
    {
        if (!File.Exists(entry.FilePath)) return;
        System.Diagnostics.Process.Start(
            new System.Diagnostics.ProcessStartInfo
            {
                FileName       = entry.FilePath,
                UseShellExecute = true
            });
    }

    // ─── Helper nom de fichier sûr ────────────────────────────────────────
    private static string SafeFileName(string s)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var clean   = new string(s.Select(c => (invalid.Contains(c) || c == ' ') ? '_' : c).ToArray());
        return clean.Length > 28 ? clean[..28] : clean;
    }
}

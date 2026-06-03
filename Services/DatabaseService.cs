using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using HabitatRural.Models;
using Microsoft.Data.Sqlite;

namespace HabitatRural.Services;

/// <summary>
/// نقطة دخول موحّدة لقاعدة بيانات SQLite.
/// تُنشئ الجداول عند أول تشغيل وتُهاجر البيانات من JSON إن وُجد.
/// </summary>
public class DatabaseService
{
    private static readonly Lazy<DatabaseService> _lazy = new(() => new DatabaseService());
    public static DatabaseService Instance => _lazy.Value;

    public string ConnectionString { get; }

    private DatabaseService()
    {
        var folder = SettingsService.Instance.SettingsFolder;
        var dbPath = Path.Combine(folder, "habitat.db");
        ConnectionString = $"Data Source={dbPath}";
        InitSchema();
        MigrateFromJson(folder);
    }

    // ─── Schéma ────────────────────────────────────────────────────────────
    private void InitSchema()
    {
        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Beneficiaires (
                Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                Code         TEXT NOT NULL,
                NomPrenom    TEXT NOT NULL,
                Adresse      TEXT NOT NULL DEFAULT '',
                DateDecision TEXT NOT NULL,
                NumeroCcp    TEXT NOT NULL DEFAULT '',
                CreatedAt    TEXT NOT NULL,
                UpdatedAt    TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_ben_code ON Beneficiaires(Code);

            CREATE TABLE IF NOT EXISTS Archives (
                Id             INTEGER PRIMARY KEY AUTOINCREMENT,
                BeneficiaireId INTEGER DEFAULT 0,
                NomPrenom      TEXT NOT NULL DEFAULT '',
                AnnexeType     TEXT NOT NULL,
                Tranche        TEXT NOT NULL,
                Building       TEXT NOT NULL,
                FilePath       TEXT NOT NULL,
                FileName       TEXT NOT NULL,
                GeneratedAt    TEXT NOT NULL
            );";
        cmd.ExecuteNonQuery();
    }

    // ─── Migration JSON → SQLite (une seule fois) ──────────────────────────
    private void MigrateFromJson(string folder)
    {
        var jsonFile = Path.Combine(folder, "beneficiaires.json");
        if (!File.Exists(jsonFile)) return;

        try
        {
            var json = File.ReadAllText(jsonFile);
            var opts = new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            var list = JsonSerializer.Deserialize<List<Beneficiaire>>(json, opts);
            if (list == null || list.Count == 0) { ArchiveJson(jsonFile); return; }

            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();

            // vérifier si la table est vide
            using var chk = conn.CreateCommand();
            chk.CommandText = "SELECT COUNT(*) FROM Beneficiaires";
            var count = Convert.ToInt64(chk.ExecuteScalar());
            if (count > 0) { ArchiveJson(jsonFile); return; }

            using var tx = conn.BeginTransaction();
            foreach (var b in list)
            {
                using var ins = conn.CreateCommand();
                ins.Transaction = tx;
                ins.CommandText = @"
                    INSERT OR IGNORE INTO Beneficiaires
                        (Code, NomPrenom, Adresse, DateDecision, NumeroCcp, CreatedAt, UpdatedAt)
                    VALUES ($code,$nom,$adr,$dd,$ccp,$ca,$ua)";
                ins.Parameters.AddWithValue("$code", b.Code);
                ins.Parameters.AddWithValue("$nom",  b.NomPrenom);
                ins.Parameters.AddWithValue("$adr",  b.Adresse);
                ins.Parameters.AddWithValue("$dd",   b.DateDecision.ToString("yyyy-MM-dd"));
                ins.Parameters.AddWithValue("$ccp",  b.NumeroCcp);
                ins.Parameters.AddWithValue("$ca",   b.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                ins.Parameters.AddWithValue("$ua",   b.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                ins.ExecuteNonQuery();
            }
            tx.Commit();

            ArchiveJson(jsonFile);
        }
        catch { /* ignorer silencieusement */ }
    }

    private static void ArchiveJson(string path)
    {
        try { File.Move(path, path + ".migrated", overwrite: true); } catch { }
    }
}

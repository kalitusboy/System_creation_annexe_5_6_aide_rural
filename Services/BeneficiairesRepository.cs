using System;
using System.Collections.Generic;
using System.Linq;
using HabitatRural.Models;
using Microsoft.Data.Sqlite;

namespace HabitatRural.Services;

/// <summary>
/// مستودع المستفيدين – SQLite بدلاً من JSON.
/// يدعم البحث بآخر 5 أرقام من الكود.
/// </summary>
public class BeneficiairesRepository
{
    private static readonly Lazy<BeneficiairesRepository> _lazy =
        new(() => new BeneficiairesRepository());
    public static BeneficiairesRepository Instance => _lazy.Value;

    private readonly string _cs;
    private List<Beneficiaire> _cache = new();

    private BeneficiairesRepository()
    {
        _cs = DatabaseService.Instance.ConnectionString; // déclenche Init + migration
        Reload();
    }

    public IReadOnlyList<Beneficiaire> All => _cache;

    // ─── Recherche générale (nom ou code) ─────────────────────────────────
    public List<Beneficiaire> Search(string? q)
    {
        if (string.IsNullOrWhiteSpace(q)) return new List<Beneficiaire>(_cache);
        var n = q.Trim();
        return _cache.Where(b =>
               (b.NomPrenom?.Contains(n, StringComparison.OrdinalIgnoreCase) ?? false)
            || (b.Code?.Contains(n, StringComparison.OrdinalIgnoreCase)     ?? false))
            .ToList();
    }

    // ─── Recherche par les 5 derniers chiffres du code ────────────────────
    public List<Beneficiaire> SearchByLast5(string last5)
    {
        if (string.IsNullOrWhiteSpace(last5)) return new List<Beneficiaire>();
        var t = last5.Trim();
        return _cache
            .Where(b => b.Code.Length >= t.Length && b.Code.EndsWith(t))
            .ToList();
    }

    public Beneficiaire? Get(int id) => _cache.FirstOrDefault(b => b.Id == id);

    // ─── Sauvegarde (insert ou update selon Id) ───────────────────────────
    public void Save(Beneficiaire b)
    {
        using var conn = new SqliteConnection(_cs);
        conn.Open();

        if (b.Id == 0)
        {
            b.CreatedAt = DateTime.Now;
            b.UpdatedAt = DateTime.Now;
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Beneficiaires
                    (Code, NomPrenom, Adresse, DateDecision, NumeroCcp, CreatedAt, UpdatedAt)
                VALUES ($c,$n,$a,$d,$ccp,$ca,$ua);
                SELECT last_insert_rowid();";
            BindParams(cmd, b);
            b.Id = Convert.ToInt32(cmd.ExecuteScalar());
        }
        else
        {
            b.UpdatedAt = DateTime.Now;
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE Beneficiaires
                SET Code=$c, NomPrenom=$n, Adresse=$a,
                    DateDecision=$d, NumeroCcp=$ccp, UpdatedAt=$ua
                WHERE Id=$id";
            BindParams(cmd, b);
            cmd.Parameters.AddWithValue("$id", b.Id);
            cmd.ExecuteNonQuery();
        }
        Reload();
    }

    public void Delete(int id)
    {
        using var conn = new SqliteConnection(_cs);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Beneficiaires WHERE Id=$id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
        Reload();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────
    private static void BindParams(SqliteCommand cmd, Beneficiaire b)
    {
        cmd.Parameters.AddWithValue("$c",   b.Code);
        cmd.Parameters.AddWithValue("$n",   b.NomPrenom);
        cmd.Parameters.AddWithValue("$a",   b.Adresse);
        cmd.Parameters.AddWithValue("$d",   b.DateDecision.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$ccp", b.NumeroCcp);
        cmd.Parameters.AddWithValue("$ca",  b.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
        cmd.Parameters.AddWithValue("$ua",  b.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
    }

    private void Reload()
    {
        var list = new List<Beneficiaire>();
        try
        {
            using var conn = new SqliteConnection(_cs);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id,Code,NomPrenom,Adresse,DateDecision,NumeroCcp,CreatedAt,UpdatedAt FROM Beneficiaires ORDER BY UpdatedAt DESC";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new Beneficiaire
                {
                    Id           = r.GetInt32(0),
                    Code         = r.GetString(1),
                    NomPrenom    = r.GetString(2),
                    Adresse      = r.GetString(3),
                    DateDecision = DateTime.TryParse(r.GetString(4), out var dd) ? dd : DateTime.Today,
                    NumeroCcp    = r.GetString(5),
                    CreatedAt    = DateTime.TryParse(r.GetString(6), out var ca) ? ca : DateTime.Now,
                    UpdatedAt    = DateTime.TryParse(r.GetString(7), out var ua) ? ua : DateTime.Now,
                });
            }
        }
        catch { }
        _cache = list;
    }
}

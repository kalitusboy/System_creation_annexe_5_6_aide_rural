using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using HabitatRural.Models;

namespace HabitatRural.Services;

/// <summary>
/// مستودع المستفيدين — SQLite (beneficiaires.db)
/// يدعم البحث بآخر 5 أرقام من Code bénéficiaire.
/// </summary>
public sealed class BeneficiairesRepository
{
    // ── Singleton ──────────────────────────────────────────────────────────
    private static readonly Lazy<BeneficiairesRepository> _lazy =
        new(() => new BeneficiairesRepository());
    public static BeneficiairesRepository Instance => _lazy.Value;

    private readonly string _connStr;

    private BeneficiairesRepository()
    {
        var folder = SettingsService.Instance.SettingsFolder;
        System.IO.Directory.CreateDirectory(folder);
        var dbPath = System.IO.Path.Combine(folder, "beneficiaires.db");
        _connStr = $"Data Source={dbPath}";
        Init();
    }

    // ── إنشاء الجدول ──────────────────────────────────────────────────────
    private void Init()
    {
        using var con = Open();
        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS beneficiaires (
                id             INTEGER PRIMARY KEY AUTOINCREMENT,
                code           TEXT    NOT NULL,
                nom_prenom     TEXT    NOT NULL,
                adresse        TEXT    NOT NULL DEFAULT '',
                date_decision  TEXT    NOT NULL DEFAULT '',
                numero_ccp     TEXT    NOT NULL DEFAULT '',
                created_at     TEXT    NOT NULL DEFAULT (datetime('now','localtime')),
                updated_at     TEXT    NOT NULL DEFAULT (datetime('now','localtime'))
            );
            CREATE INDEX IF NOT EXISTS idx_code ON beneficiaires(code);
            """;
        cmd.ExecuteNonQuery();
    }

    private SqliteConnection Open()
    {
        var con = new SqliteConnection(_connStr);
        con.Open();
        return con;
    }

    // ── قراءة ─────────────────────────────────────────────────────────────
    public List<Beneficiaire> GetAll()
    {
        using var con = Open();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT * FROM beneficiaires ORDER BY nom_prenom";
        return ReadList(cmd);
    }

    /// <summary>
    /// بحث بالاسم، أو الكود كاملاً، أو آخر 5 أرقام من الكود.
    /// </summary>
    public List<Beneficiaire> Search(string? query)
    {
        if (string.IsNullOrWhiteSpace(query)) return GetAll();

        using var con = Open();
        using var cmd = con.CreateCommand();
        var q = query.Trim();
        cmd.CommandText = """
            SELECT * FROM beneficiaires
            WHERE  nom_prenom  LIKE @q
               OR  code        LIKE @q
               OR  SUBSTR(code, LENGTH(code)-4, 5) = @exact
            ORDER BY nom_prenom
            """;
        cmd.Parameters.AddWithValue("@q",     $"%{q}%");
        cmd.Parameters.AddWithValue("@exact", q);
        return ReadList(cmd);
    }

    public Beneficiaire? GetById(int id)
    {
        using var con = Open();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT * FROM beneficiaires WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        var list = ReadList(cmd);
        return list.Count > 0 ? list[0] : null;
    }

    // ── حفظ (إضافة أو تعديل) ─────────────────────────────────────────────
    public void Save(Beneficiaire b)
    {
        using var con = Open();
        using var cmd = con.CreateCommand();
        var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        if (b.Id == 0)
        {
            cmd.CommandText = """
                INSERT INTO beneficiaires
                    (code, nom_prenom, adresse, date_decision, numero_ccp, created_at, updated_at)
                VALUES
                    (@code, @nom, @adr, @dat, @ccp, @now, @now);
                SELECT last_insert_rowid();
                """;
        }
        else
        {
            cmd.CommandText = """
                INSERT INTO beneficiaires
                    (id, code, nom_prenom, adresse, date_decision, numero_ccp, created_at, updated_at)
                VALUES
                    (@id, @code, @nom, @adr, @dat, @ccp, @now, @now)
                ON CONFLICT(id) DO UPDATE SET
                    code          = excluded.code,
                    nom_prenom    = excluded.nom_prenom,
                    adresse       = excluded.adresse,
                    date_decision = excluded.date_decision,
                    numero_ccp    = excluded.numero_ccp,
                    updated_at    = excluded.updated_at;
                SELECT @id;
                """;
            cmd.Parameters.AddWithValue("@id", b.Id);
        }

        cmd.Parameters.AddWithValue("@code", b.Code);
        cmd.Parameters.AddWithValue("@nom",  b.NomPrenom);
        cmd.Parameters.AddWithValue("@adr",  b.Adresse);
        cmd.Parameters.AddWithValue("@dat",  b.DateDecision.ToString("dd/MM/yyyy"));
        cmd.Parameters.AddWithValue("@ccp",  b.NumeroCcp);
        cmd.Parameters.AddWithValue("@now",  now);

        var result = cmd.ExecuteScalar();
        if (result != null && b.Id == 0)
            b.Id = Convert.ToInt32(result);
    }

    // ── حذف ───────────────────────────────────────────────────────────────
    public void Delete(int id)
    {
        using var con = Open();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "DELETE FROM beneficiaires WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    // ── تحويل الصف ────────────────────────────────────────────────────────
    private static List<Beneficiaire> ReadList(SqliteCommand cmd)
    {
        var list = new List<Beneficiaire>();
        using var rd = cmd.ExecuteReader();
        while (rd.Read())
        {
            var dateStr = rd["date_decision"].ToString() ?? "";
            DateTime.TryParseExact(dateStr, "dd/MM/yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var date);

            list.Add(new Beneficiaire
            {
                Id           = Convert.ToInt32(rd["id"]),
                Code         = rd["code"].ToString()      ?? "",
                NomPrenom    = rd["nom_prenom"].ToString() ?? "",
                Adresse      = rd["adresse"].ToString()   ?? "",
                DateDecision = date == default ? DateTime.Today : date,
                NumeroCcp    = rd["numero_ccp"].ToString() ?? "",
            });
        }
        return list;
    }
}

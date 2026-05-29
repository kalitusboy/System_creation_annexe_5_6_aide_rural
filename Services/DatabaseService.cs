using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HabitatRural.Models;

namespace HabitatRural.Services;

public class DatabaseService
{
    private static readonly Lazy<DatabaseService> _lazy = new(() => new DatabaseService());
    public static DatabaseService Instance => _lazy.Value;

    private readonly string _connectionString;
    private DatabaseService()
    {
        string dbFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HabitatRural");
        Directory.CreateDirectory(dbFolder);
        string dbPath = Path.Combine(dbFolder, "habitat.db");
        _connectionString = $"Data Source={dbPath}";
    }

    public void Initialize()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        string sql = @"
            CREATE TABLE IF NOT EXISTS Beneficiaires (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Code TEXT NOT NULL UNIQUE,
                NomPrenom TEXT NOT NULL,
                Adresse TEXT NOT NULL,
                NumeroCcp TEXT NOT NULL,
                DateDecision TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS Archives (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                BeneficiaireId INTEGER NOT NULL,
                TypeAnnexe TEXT NOT NULL,
                Tranche TEXT NOT NULL,
                DateGeneration TEXT NOT NULL,
                CheminFichier TEXT NOT NULL,
                FOREIGN KEY(BeneficiaireId) REFERENCES Beneficiaires(Id) ON DELETE CASCADE
            );
        ";
        using var cmd = new SqliteCommand(sql, conn);
        cmd.ExecuteNonQuery();
    }

    public List<Beneficiaire> GetAllBeneficiaires()
    {
        var list = new List<Beneficiaire>();
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = new SqliteCommand("SELECT * FROM Beneficiaires ORDER BY NomPrenom", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            list.Add(MapBeneficiaire(reader));
        return list;
    }

    public List<Beneficiaire> SearchBeneficiaires(string? term)
    {
        var list = new List<Beneficiaire>();
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        string sql = "SELECT * FROM Beneficiaires WHERE 1=0";
        if (!string.IsNullOrWhiteSpace(term))
        {
            sql = "SELECT * FROM Beneficiaires WHERE NomPrenom LIKE @term OR Code LIKE @term OR substr(Code, -5) = @last5 ORDER BY NomPrenom";
        }
        else
        {
            sql = "SELECT * FROM Beneficiaires ORDER BY NomPrenom";
        }
        using var cmd = new SqliteCommand(sql, conn);
        if (!string.IsNullOrWhiteSpace(term))
        {
            cmd.Parameters.AddWithValue("@term", $"%{term}%");
            cmd.Parameters.AddWithValue("@last5", term);
        }
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            list.Add(MapBeneficiaire(reader));
        return list;
    }

    public Beneficiaire? GetBeneficiaireById(int id)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = new SqliteCommand("SELECT * FROM Beneficiaires WHERE Id = @id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? MapBeneficiaire(reader) : null;
    }

    public void SaveBeneficiaire(Beneficiaire b)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        if (b.Id == 0)
        {
            string sql = @"INSERT INTO Beneficiaires (Code, NomPrenom, Adresse, NumeroCcp, DateDecision, CreatedAt, UpdatedAt)
                           VALUES (@code, @nom, @adr, @ccp, @dec, @created, @updated)";
            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@code", b.Code);
            cmd.Parameters.AddWithValue("@nom", b.NomPrenom);
            cmd.Parameters.AddWithValue("@adr", b.Adresse);
            cmd.Parameters.AddWithValue("@ccp", b.NumeroCcp);
            cmd.Parameters.AddWithValue("@dec", b.DateDecision.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@created", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@updated", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.ExecuteNonQuery();
            b.Id = (int)conn.LastInsertRowId;
        }
        else
        {
            string sql = @"UPDATE Beneficiaires SET Code = @code, NomPrenom = @nom, Adresse = @adr,
                           NumeroCcp = @ccp, DateDecision = @dec, UpdatedAt = @updated WHERE Id = @id";
            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@code", b.Code);
            cmd.Parameters.AddWithValue("@nom", b.NomPrenom);
            cmd.Parameters.AddWithValue("@adr", b.Adresse);
            cmd.Parameters.AddWithValue("@ccp", b.NumeroCcp);
            cmd.Parameters.AddWithValue("@dec", b.DateDecision.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@updated", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@id", b.Id);
            cmd.ExecuteNonQuery();
        }
    }

    public void DeleteBeneficiaire(int id)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = new SqliteCommand("DELETE FROM Beneficiaires WHERE Id = @id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    public void SaveArchive(ArchiveEntry archive)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        string sql = @"INSERT INTO Archives (BeneficiaireId, TypeAnnexe, Tranche, DateGeneration, CheminFichier)
                       VALUES (@bid, @type, @tranche, @date, @path)";
        using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@bid", archive.BeneficiaireId);
        cmd.Parameters.AddWithValue("@type", archive.TypeAnnexe);
        cmd.Parameters.AddWithValue("@tranche", archive.Tranche);
        cmd.Parameters.AddWithValue("@date", archive.DateGeneration.ToString("yyyy-MM-dd HH:mm:ss"));
        cmd.Parameters.AddWithValue("@path", archive.CheminFichier);
        cmd.ExecuteNonQuery();
    }

    public List<ArchiveEntry> GetArchivesForBeneficiaire(int benefId)
    {
        var list = new List<ArchiveEntry>();
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = new SqliteCommand("SELECT * FROM Archives WHERE BeneficiaireId = @bid ORDER BY DateGeneration DESC", conn);
        cmd.Parameters.AddWithValue("@bid", benefId);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new ArchiveEntry
            {
                Id = reader.GetInt32(0),
                BeneficiaireId = reader.GetInt32(1),
                TypeAnnexe = reader.GetString(2),
                Tranche = reader.GetString(3),
                DateGeneration = DateTime.Parse(reader.GetString(4)),
                CheminFichier = reader.GetString(5)
            });
        }
        return list;
    }

    private static Beneficiaire MapBeneficiaire(SqliteDataReader r)
    {
        return new Beneficiaire
        {
            Id = r.GetInt32(0),
            Code = r.GetString(1),
            NomPrenom = r.GetString(2),
            Adresse = r.GetString(3),
            NumeroCcp = r.GetString(4),
            DateDecision = DateTime.Parse(r.GetString(5)),
            CreatedAt = DateTime.Parse(r.GetString(6)),
            UpdatedAt = DateTime.Parse(r.GetString(7))
        };
    }
}

public class ArchiveEntry
{
    public int Id { get; set; }
    public int BeneficiaireId { get; set; }
    public string TypeAnnexe { get; set; } = "";
    public string Tranche { get; set; } = "";
    public DateTime DateGeneration { get; set; }
    public string CheminFichier { get; set; } = "";
}

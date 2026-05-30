using System;
using System.IO;
using HabitatRural.Models;
using Microsoft.Data.Sqlite;

namespace HabitatRural.Services {
    public class DatabaseService {
        private static readonly Lazy<DatabaseService> _lazy = new(() => new DatabaseService());
        public static DatabaseService Instance => _lazy.Value;
        private readonly string _dbPath;
        private readonly string _connectionString;
        private DatabaseService() {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string dbFolder = Path.Combine(appData, "HabitatRural");
            Directory.CreateDirectory(dbFolder);
            _dbPath = Path.Combine(dbFolder, "beneficiaires.db");
            _connectionString = $"Data Source={_dbPath}";
            InitializeDatabase();
        }
        private void InitializeDatabase() {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            string sql = @"CREATE TABLE IF NOT EXISTS Beneficiaires (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Code TEXT NOT NULL UNIQUE,
                NomPrenom TEXT NOT NULL,
                Adresse TEXT NOT NULL,
                NumeroCcp TEXT NOT NULL,
                DateDecision TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL)";
            using var cmd = new SqliteCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }
        public Beneficiaire? SearchByLast5(string last5) {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            string query = "SELECT * FROM Beneficiaires WHERE Code LIKE @suffix";
            using var cmd = new SqliteCommand(query, conn);
            cmd.Parameters.AddWithValue("@suffix", $"%{last5}");
            using var reader = cmd.ExecuteReader();
            if (reader.Read()) return Map(reader);
            return null;
        }
        public void SaveBeneficiaire(Beneficiaire b) {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            string query = @"INSERT OR REPLACE INTO Beneficiaires 
                (Id, Code, NomPrenom, Adresse, NumeroCcp, DateDecision, CreatedAt, UpdatedAt)
                VALUES (@Id, @Code, @NomPrenom, @Adresse, @NumeroCcp, @DateDecision, @CreatedAt, @UpdatedAt)";
            using var cmd = new SqliteCommand(query, conn);
            cmd.Parameters.AddWithValue("@Id", b.Id);
            cmd.Parameters.AddWithValue("@Code", b.Code);
            cmd.Parameters.AddWithValue("@NomPrenom", b.NomPrenom);
            cmd.Parameters.AddWithValue("@Adresse", b.Adresse);
            cmd.Parameters.AddWithValue("@NumeroCcp", b.NumeroCcp);
            cmd.Parameters.AddWithValue("@DateDecision", b.DateDecision.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@CreatedAt", b.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@UpdatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.ExecuteNonQuery();
        }
        private Beneficiaire Map(SqliteDataReader reader) => new Beneficiaire {
            Id = reader.GetInt32(0),
            Code = reader.GetString(1),
            NomPrenom = reader.GetString(2),
            Adresse = reader.GetString(3),
            NumeroCcp = reader.GetString(4),
            DateDecision = DateTime.Parse(reader.GetString(5)),
            CreatedAt = DateTime.Parse(reader.GetString(6)),
            UpdatedAt = DateTime.Parse(reader.GetString(7))
        };
    }
}
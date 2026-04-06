using System.IO;
using Microsoft.Data.Sqlite;

namespace Haystack.Data;

public static class Database
{
    private static string? _connectionString;

    public static void Initialize()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(appData, "Haystack");
        Directory.CreateDirectory(dir);

        var dbPath = Path.Combine(dir, "pulsepoint.db");
        _connectionString = $"Data Source={dbPath}";

        using var conn = GetConnection();
        ApplyMigrations(conn);
    }

    public static SqliteConnection GetConnection()
    {
        if (_connectionString is null)
            throw new InvalidOperationException("Database not initialized.");
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }

    private static void ApplyMigrations(SqliteConnection conn)
    {
        conn.Execute(@"
            CREATE TABLE IF NOT EXISTS contacts (
                id          INTEGER PRIMARY KEY AUTOINCREMENT,
                name        TEXT NOT NULL,
                company     TEXT,
                type        INTEGER NOT NULL DEFAULT 0,
                status      INTEGER NOT NULL DEFAULT 0,
                phone       TEXT,
                email       TEXT,
                city        TEXT,
                state       TEXT,
                notes       TEXT,
                created_at  TEXT NOT NULL DEFAULT (datetime('now')),
                updated_at  TEXT NOT NULL DEFAULT (datetime('now'))
            );

            CREATE TABLE IF NOT EXISTS interactions (
                id                  INTEGER PRIMARY KEY AUTOINCREMENT,
                contact_id          INTEGER NOT NULL REFERENCES contacts(id) ON DELETE CASCADE,
                type                INTEGER NOT NULL DEFAULT 4,
                date                TEXT NOT NULL,
                summary             TEXT,
                next_followup_date  TEXT,
                created_at          TEXT NOT NULL DEFAULT (datetime('now'))
            );

            CREATE TABLE IF NOT EXISTS contact_custom_fields (
                id          INTEGER PRIMARY KEY AUTOINCREMENT,
                contact_id  INTEGER NOT NULL REFERENCES contacts(id) ON DELETE CASCADE,
                key         TEXT NOT NULL,
                value       TEXT
            );

            CREATE INDEX IF NOT EXISTS idx_interactions_contact ON interactions(contact_id);
            CREATE INDEX IF NOT EXISTS idx_interactions_date    ON interactions(date);
            CREATE INDEX IF NOT EXISTS idx_custom_fields_contact ON contact_custom_fields(contact_id);
        ");
    }
}

// Minimal extension so we can call conn.Execute() without Dapper for DDL
file static class ConnectionExtensions
{
    public static void Execute(this SqliteConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }
}

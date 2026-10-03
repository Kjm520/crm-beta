using System.IO;
using Microsoft.Data.Sqlite;

namespace Haystack.Data;

public static class Database
{
    private static string? _connectionString;

    /// <summary>Full path to the live database file. Set during Initialize().</summary>
    public static string DbPath { get; private set; } = string.Empty;

    public static void Initialize()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(appData, "Haystack");
        Directory.CreateDirectory(dir);

        var dbPath = Path.Combine(dir, "haystack.db");
        DbPath = dbPath;
        _connectionString = $"Data Source={dbPath}";

        // Teach Dapper how to read SQLite's TEXT dates as DateTime (see class docs).
        SqliteDateTimeHandlers.Register();

        using var conn = GetConnection();
        Migrations.Apply(conn);
    }

    public static SqliteConnection GetConnection()
    {
        if (_connectionString is null)
            throw new InvalidOperationException("Database not initialized.");
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        // PRAGMA foreign_keys is PER-CONNECTION, and every repository call opens a
        // fresh connection - so it must be set here, not just once at startup.
        // Without this, ON DELETE CASCADE / SET NULL silently never fire.
        conn.Execute("PRAGMA foreign_keys = ON;");
        return conn;
    }

    /// <summary>
    /// Writes a clean, consistent snapshot of the database to <paramref name="destinationPath"/>.
    /// Uses SQLite's VACUUM INTO, which produces a fully intact copy even while the
    /// database is in use · safer than a raw file copy, which can catch in-flight writes.
    /// </summary>
    public static void Backup(string destinationPath)
    {
        // VACUUM INTO fails if the target file already exists, so overwrite cleanly.
        if (File.Exists(destinationPath))
            File.Delete(destinationPath);

        using var conn = GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "VACUUM INTO $dest;";
        cmd.Parameters.AddWithValue("$dest", destinationPath);
        cmd.ExecuteNonQuery();
    }
}

// Minimal extension so we can run raw SQL without Dapper for DDL/PRAGMAs.
internal static class ConnectionExtensions
{
    public static void Execute(this SqliteConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    public static long ExecuteScalarLong(this SqliteConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        return Convert.ToInt64(cmd.ExecuteScalar());
    }
}

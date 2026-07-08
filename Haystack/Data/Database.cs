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

        var dbPath = Path.Combine(dir, "haystack.db");
        _connectionString = $"Data Source={dbPath}";

        using var conn = GetConnection();
        conn.Execute("PRAGMA foreign_keys = ON;");
        Migrations.Apply(conn);
    }

    public static SqliteConnection GetConnection()
    {
        if (_connectionString is null)
            throw new InvalidOperationException("Database not initialized.");
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
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

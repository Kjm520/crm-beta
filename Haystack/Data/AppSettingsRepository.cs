using Dapper;

namespace Haystack.Data;

/// <summary>
/// A simple key-value store for app-level preferences (nav order, and whatever
/// settings come later). Values are stored as strings; callers encode/decode.
/// </summary>
public class AppSettingsRepository
{
    public string? Get(string key)
    {
        using var conn = Database.GetConnection();
        return conn.QueryFirstOrDefault<string>(
            "SELECT value FROM app_settings WHERE key = @key", new { key });
    }

    public void Set(string key, string value)
    {
        using var conn = Database.GetConnection();
        conn.Execute(@"
            INSERT INTO app_settings (key, value) VALUES (@key, @value)
            ON CONFLICT(key) DO UPDATE SET value = excluded.value
        ", new { key, value });
    }
}

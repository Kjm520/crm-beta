using Microsoft.Data.Sqlite;

namespace Haystack.Data;

/// <summary>
/// Versioned schema migrations. Each entry in <see cref="_migrations"/> is one
/// forward step, keyed by its target schema version. On startup the runner reads
/// SQLite's built-in <c>user_version</c> and applies every step whose version is
/// higher than the DB's current version, in order, each inside its own transaction.
///
/// This is what makes updates safe: shipping a structural change is just adding a
/// new numbered step here. Every user's database - no matter how old - upgrades
/// itself on next launch, preserving all existing data. NEVER edit or reorder an
/// already-released migration; only append new ones with the next number.
/// </summary>
internal static class Migrations
{
    // version number -> SQL to run to reach that version.
    private static readonly SortedDictionary<int, string> _migrations = new()
    {
        // v1: initial schema (contacts, interactions, custom fields).
        [1] = """
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

            CREATE INDEX IF NOT EXISTS idx_interactions_contact  ON interactions(contact_id);
            CREATE INDEX IF NOT EXISTS idx_interactions_date     ON interactions(date);
            CREATE INDEX IF NOT EXISTS idx_custom_fields_contact ON contact_custom_fields(contact_id);
            """,

        // Future structural changes go here as new numbered steps, e.g.:
        // [2] = "ALTER TABLE contacts ADD COLUMN website TEXT;",
        // [3] = "CREATE TABLE tasks (...);",
    };

    public static void Apply(SqliteConnection conn)
    {
        var current = conn.ExecuteScalarLong("PRAGMA user_version;");

        foreach (var (version, sql) in _migrations)
        {
            if (version <= current) continue;

            using var tx = conn.BeginTransaction();
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
            }

            // user_version doesn't accept a bound parameter; version is an int
            // we control (dictionary key), so interpolation is safe here.
            using (var bump = conn.CreateCommand())
            {
                bump.Transaction = tx;
                bump.CommandText = $"PRAGMA user_version = {version};";
                bump.ExecuteNonQuery();
            }

            tx.Commit();
        }
    }
}

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

        // v2: introduce companies as a parent entity; contacts now belong to a
        // company (required company_id FK). Existing test data is intentionally
        // dropped (agreed clean-slate) · safe because no real data existed yet.
        [2] = """
            DROP TABLE IF EXISTS interactions;
            DROP TABLE IF EXISTS contact_custom_fields;
            DROP TABLE IF EXISTS contacts;

            CREATE TABLE companies (
                id          INTEGER PRIMARY KEY AUTOINCREMENT,
                name        TEXT NOT NULL,
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

            CREATE TABLE contacts (
                id          INTEGER PRIMARY KEY AUTOINCREMENT,
                company_id  INTEGER NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
                name        TEXT NOT NULL,
                title       TEXT,
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

            CREATE TABLE interactions (
                id                  INTEGER PRIMARY KEY AUTOINCREMENT,
                contact_id          INTEGER NOT NULL REFERENCES contacts(id) ON DELETE CASCADE,
                type                INTEGER NOT NULL DEFAULT 4,
                date                TEXT NOT NULL,
                summary             TEXT,
                next_followup_date  TEXT,
                created_at          TEXT NOT NULL DEFAULT (datetime('now'))
            );

            CREATE INDEX idx_contacts_company     ON contacts(company_id);
            CREATE INDEX idx_interactions_contact ON interactions(contact_id);
            CREATE INDEX idx_interactions_date    ON interactions(date);
            """,

        // v3: leads · a flat, standalone bulk list (deliberately NOT linked to
        // companies/contacts). Statuses are user-editable data, not a C# enum, so
        // they live in their own table. status_id is nullable with ON DELETE SET NULL
        // so removing a status never destroys leads.
        [3] = """
            CREATE TABLE lead_statuses (
                id          INTEGER PRIMARY KEY AUTOINCREMENT,
                name        TEXT NOT NULL UNIQUE,
                sort_order  INTEGER NOT NULL DEFAULT 0
            );

            INSERT INTO lead_statuses (name, sort_order) VALUES
                ('New', 0),
                ('Contacted', 1),
                ('Dead', 2);

            CREATE TABLE leads (
                id          INTEGER PRIMARY KEY AUTOINCREMENT,
                name        TEXT,
                company     TEXT,
                phone       TEXT,
                email       TEXT,
                status_id   INTEGER REFERENCES lead_statuses(id) ON DELETE SET NULL,
                notes       TEXT,
                created_at  TEXT NOT NULL DEFAULT (datetime('now')),
                updated_at  TEXT NOT NULL DEFAULT (datetime('now'))
            );

            CREATE INDEX idx_leads_status  ON leads(status_id);
            CREATE INDEX idx_leads_created ON leads(created_at);
            """,

        // v4: lead -> company conversion. A converted lead is KEPT (it's proof the
        // cold call worked) and linked to the company it became.
        //
        // is_won marks the "converted" status structurally rather than by name, so
        // the user can freely rename it in Settings without breaking conversion.
        [4] = """
            ALTER TABLE lead_statuses ADD COLUMN is_won INTEGER NOT NULL DEFAULT 0;

            INSERT INTO lead_statuses (name, sort_order, is_won) VALUES ('Won', 3, 1);

            ALTER TABLE leads ADD COLUMN converted_company_id INTEGER
                REFERENCES companies(id) ON DELETE SET NULL;

            CREATE INDEX idx_leads_converted ON leads(converted_company_id);
            """,

        // v5: generic key-value store for app-level preferences (first use: nav tab
        // order). Reusable for future settings; travels with the DB backup.
        [5] = """
            CREATE TABLE app_settings (
                key    TEXT PRIMARY KEY,
                value  TEXT
            );
            """,

        // v6: drop the "Vendor" contact type. It was the last member of the
        // ContactType enum (value 3), so removing it from C# leaves 0-2 untouched -
        // but any row already stored as 3 would deserialize to an undefined enum
        // value. Re-home those rows on Customer (0) so no record is left dangling.
        [6] = """
            UPDATE companies SET type = 0 WHERE type = 3;
            UPDATE contacts  SET type = 0 WHERE type = 3;
            """,

        // v7: merge city + state into one free-text `location` field. Two columns
        // implied a structure the data never had (no validation, no lookups on
        // state) and cost two inputs to fill; one "Austin, TX" box matches how the
        // user actually thinks about it. Backfill joins the old pair with ", ",
        // tolerating either side being empty, then leaves NULL when both were.
        [7] = """
            ALTER TABLE companies ADD COLUMN location TEXT;
            UPDATE companies SET location =
                COALESCE(NULLIF(TRIM(city), ''), '')
                || CASE WHEN NULLIF(TRIM(city), '') IS NOT NULL
                         AND NULLIF(TRIM(state), '') IS NOT NULL
                        THEN ', ' ELSE '' END
                || COALESCE(NULLIF(TRIM(state), ''), '');
            UPDATE companies SET location = NULL WHERE location = '';
            ALTER TABLE companies DROP COLUMN city;
            ALTER TABLE companies DROP COLUMN state;

            ALTER TABLE contacts ADD COLUMN location TEXT;
            UPDATE contacts SET location =
                COALESCE(NULLIF(TRIM(city), ''), '')
                || CASE WHEN NULLIF(TRIM(city), '') IS NOT NULL
                         AND NULLIF(TRIM(state), '') IS NOT NULL
                        THEN ', ' ELSE '' END
                || COALESCE(NULLIF(TRIM(state), ''), '');
            UPDATE contacts SET location = NULL WHERE location = '';
            ALTER TABLE contacts DROP COLUMN city;
            ALTER TABLE contacts DROP COLUMN state;
            """,

        // v8: closed lead statuses. "Closed" means finished · won, lost, dead -
        // so the Leads list can hide those rows by default. A flag rather than a
        // name match (same reasoning as is_won) so statuses can be renamed and
        // user-created ones marked closed in Settings. Backfill: the won status
        // is closed by definition; 'Dead' is the seeded terminal status, and
        // 'Lost' is matched in case the user already added one by that name.
        [8] = """
            ALTER TABLE lead_statuses ADD COLUMN is_closed INTEGER NOT NULL DEFAULT 0;
            UPDATE lead_statuses SET is_closed = 1
            WHERE is_won = 1 OR name COLLATE NOCASE IN ('dead', 'lost');
            """,

        // v9: companies no longer offer the "Carrier" type (value 2) · that type
        // is for contacts. Re-home any company stored as Carrier on Customer (0)
        // so no row holds a choice the form can't display (same treatment Vendor
        // got in v6). Contacts keep Carrier untouched.
        [9] = """
            UPDATE companies SET type = 0 WHERE type = 2;
            """,

        // v10: relationship-monitoring cadence. "Contact every N days" per company
        // (activity on it or any of its contacts counts) and per contact. NULL =
        // not monitored; the alert bell derives overdue alerts from this at
        // runtime · alerts themselves are never stored.
        [10] = """
            ALTER TABLE companies ADD COLUMN contact_every_days INTEGER;
            ALTER TABLE contacts  ADD COLUMN contact_every_days INTEGER;
            """,

        // v11: baseline "Contact created" note (type 4 = Note) for every contact
        // that has no interactions, dated at its creation. New contacts get this
        // note at save time (ContactDetailViewModel); this backfills existing
        // ones so day-since-contact math is always anchored in real data and the
        // "never contacted" int.MaxValue sentinel can't leak into stale lists.
        [11] = """
            INSERT INTO interactions (contact_id, type, date, summary, created_at)
            SELECT id, 4, created_at, 'Contact created', datetime('now')
            FROM contacts
            WHERE id NOT IN (SELECT DISTINCT contact_id FROM interactions);
            """,

        // v12: the Workflow queue · free-text tasks worked oldest-first (new ones
        // append to the bottom of the list, so the longest-outstanding item sits
        // on top). Completing a task sets done_at rather than deleting the row:
        // recoverable, and a "recently completed" view stays possible later.
        [12] = """
            CREATE TABLE workflow_tasks (
                id          INTEGER PRIMARY KEY AUTOINCREMENT,
                text        TEXT NOT NULL,
                created_at  TEXT NOT NULL DEFAULT (datetime('now')),
                done_at     TEXT
            );

            CREATE INDEX idx_workflow_open ON workflow_tasks(done_at, created_at);
            """,

        // v13: monitor-created tasks. Monitors (Alerts/MonitorEngine) materialize
        // tasks into the queue automatically. `source` stamps the condition that
        // created one (e.g. 'cadence:company:12') · it's the dedup key and how
        // the monitor finds its tasks again to auto-resolve them; NULL = typed by
        // hand. target_kind/target_id (RecordKind + row id) make the task click
        // through to its record.
        [13] = """
            ALTER TABLE workflow_tasks ADD COLUMN source TEXT;
            ALTER TABLE workflow_tasks ADD COLUMN target_kind INTEGER;
            ALTER TABLE workflow_tasks ADD COLUMN target_id INTEGER;

            CREATE INDEX idx_workflow_task_source ON workflow_tasks(source) WHERE source IS NOT NULL;
            """,
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

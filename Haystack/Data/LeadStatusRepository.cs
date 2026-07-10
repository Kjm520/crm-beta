using Dapper;
using Haystack.Models;

namespace Haystack.Data;

public class LeadStatusRepository
{
    public IEnumerable<LeadStatus> GetAll()
    {
        using var conn = Database.GetConnection();
        return conn.Query<LeadStatus>(@"
            SELECT
                s.id                 AS Id,
                s.name               AS Name,
                s.sort_order         AS SortOrder,
                s.is_won             AS IsWon,
                COUNT(l.id)          AS LeadCount
            FROM lead_statuses s
            LEFT JOIN leads l ON l.status_id = s.id
            GROUP BY s.id
            ORDER BY s.sort_order, s.name
        ");
    }

    /// <summary>
    /// The status leads move to on conversion, found by its is_won flag rather than
    /// its name (so the user can rename it). Null if the user deleted it.
    /// </summary>
    public LeadStatus? GetWonStatus()
    {
        using var conn = Database.GetConnection();
        return conn.QueryFirstOrDefault<LeadStatus>(@"
            SELECT id AS Id, name AS Name, sort_order AS SortOrder, is_won AS IsWon
            FROM lead_statuses
            WHERE is_won = 1
            LIMIT 1
        ");
    }

    public int Insert(LeadStatus status)
    {
        using var conn = Database.GetConnection();
        return conn.ExecuteScalar<int>(@"
            INSERT INTO lead_statuses (name, sort_order, is_won)
            VALUES (@Name, @SortOrder, @IsWon);
            SELECT last_insert_rowid();
        ", status);
    }

    public void Update(LeadStatus status)
    {
        using var conn = Database.GetConnection();
        conn.Execute(@"
            UPDATE lead_statuses SET
                name       = @Name,
                sort_order = @SortOrder
            WHERE id = @Id
        ", status);
    }

    /// <summary>
    /// Deletes a status. Leads using it keep existing; their status_id becomes NULL
    /// (ON DELETE SET NULL), so no lead data is lost.
    /// </summary>
    public void Delete(int id)
    {
        using var conn = Database.GetConnection();
        conn.Execute("DELETE FROM lead_statuses WHERE id = @id", new { id });
    }

    /// <summary>True if another status already uses this name (names are UNIQUE).</summary>
    public bool NameExists(string name, int excludeId = 0)
    {
        using var conn = Database.GetConnection();
        return conn.ExecuteScalar<int>(@"
            SELECT COUNT(*) FROM lead_statuses
            WHERE name = @name COLLATE NOCASE AND id <> @excludeId
        ", new { name, excludeId }) > 0;
    }
}

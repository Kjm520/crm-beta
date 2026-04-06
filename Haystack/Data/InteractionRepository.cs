using Dapper;
using Haystack.Models;

namespace Haystack.Data;

public class InteractionRepository
{
    public IEnumerable<Interaction> GetByContact(int contactId)
    {
        using var conn = Database.GetConnection();
        return conn.Query<Interaction>(@"
            SELECT
                id                  AS Id,
                contact_id          AS ContactId,
                type                AS Type,
                date                AS Date,
                summary             AS Summary,
                next_followup_date  AS NextFollowUpDate,
                created_at          AS CreatedAt
            FROM interactions
            WHERE contact_id = @contactId
            ORDER BY date DESC
        ", new { contactId });
    }

    public IEnumerable<Interaction> GetRecent(int count = 20)
    {
        using var conn = Database.GetConnection();
        return conn.Query<Interaction>(@"
            SELECT
                i.id                  AS Id,
                i.contact_id          AS ContactId,
                i.type                AS Type,
                i.date                AS Date,
                i.summary             AS Summary,
                i.next_followup_date  AS NextFollowUpDate,
                i.created_at          AS CreatedAt,
                c.name                AS ContactName
            FROM interactions i
            JOIN contacts c ON c.id = i.contact_id
            ORDER BY i.date DESC
            LIMIT @count
        ", new { count });
    }

    public IEnumerable<Interaction> GetUpcomingFollowUps()
    {
        using var conn = Database.GetConnection();
        return conn.Query<Interaction>(@"
            SELECT
                i.id                  AS Id,
                i.contact_id          AS ContactId,
                i.type                AS Type,
                i.date                AS Date,
                i.summary             AS Summary,
                i.next_followup_date  AS NextFollowUpDate,
                i.created_at          AS CreatedAt,
                c.name                AS ContactName
            FROM interactions i
            JOIN contacts c ON c.id = i.contact_id
            WHERE i.next_followup_date IS NOT NULL
              AND i.next_followup_date >= date('now')
            ORDER BY i.next_followup_date ASC
        ");
    }

    public int Insert(Interaction interaction)
    {
        using var conn = Database.GetConnection();
        return conn.ExecuteScalar<int>(@"
            INSERT INTO interactions (contact_id, type, date, summary, next_followup_date, created_at)
            VALUES (@ContactId, @Type, @Date, @Summary, @NextFollowUpDate, datetime('now'));
            SELECT last_insert_rowid();
        ", interaction);
    }

    public void Delete(int id)
    {
        using var conn = Database.GetConnection();
        conn.Execute("DELETE FROM interactions WHERE id = @id", new { id });
    }
}

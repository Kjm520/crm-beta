using Dapper;
using Haystack.Models;

namespace Haystack.Data;

public class LeadRepository
{
    // LEFT JOIN (not JOIN) so leads with no status - or whose status was deleted -
    // still appear in the list.
    private const string SelectLead = @"
        SELECT
            l.id                   AS Id,
            l.name                 AS Name,
            l.company              AS Company,
            l.phone                AS Phone,
            l.email                AS Email,
            l.status_id            AS StatusId,
            s.name                 AS StatusName,
            COALESCE(s.is_closed, 0) AS StatusIsClosed,
            l.notes                AS Notes,
            l.converted_company_id AS ConvertedCompanyId,
            l.created_at           AS CreatedAt,
            l.updated_at           AS UpdatedAt
        FROM leads l
        LEFT JOIN lead_statuses s ON s.id = l.status_id ";

    public IEnumerable<Lead> GetAll()
    {
        using var conn = Database.GetConnection();
        return conn.Query<Lead>(SelectLead + "ORDER BY l.created_at DESC");
    }

    public Lead? GetById(int id)
    {
        using var conn = Database.GetConnection();
        return conn.QueryFirstOrDefault<Lead>(SelectLead + "WHERE l.id = @id", new { id });
    }

    public IEnumerable<Lead> Search(string query)
    {
        using var conn = Database.GetConnection();
        var like = $"%{query}%";

        // Phones are stored as bare digits, so a search like "402-720" would miss.
        // Match phone against a digits-only form of the query as well.
        var digits = new string(query.Where(char.IsDigit).ToArray());
        var digitsLike = string.IsNullOrEmpty(digits) ? null : $"%{digits}%";

        return conn.Query<Lead>(SelectLead + @"
            WHERE l.name    LIKE @like
               OR l.company LIKE @like
               OR l.email   LIKE @like
               OR l.notes   LIKE @like
               OR l.phone   LIKE @like
               OR (@digitsLike IS NOT NULL AND l.phone LIKE @digitsLike)
            ORDER BY l.created_at DESC
        ", new { like, digitsLike });
    }

    public int Insert(Lead lead)
    {
        using var conn = Database.GetConnection();
        return conn.ExecuteScalar<int>(@"
            INSERT INTO leads (name, company, phone, email, status_id, notes, created_at, updated_at)
            VALUES (@Name, @Company, @Phone, @Email, @StatusId, @Notes, datetime('now'), datetime('now'));
            SELECT last_insert_rowid();
        ", lead);
    }

    public void Update(Lead lead)
    {
        using var conn = Database.GetConnection();
        conn.Execute(@"
            UPDATE leads SET
                name       = @Name,
                company    = @Company,
                phone      = @Phone,
                email      = @Email,
                status_id  = @StatusId,
                notes      = @Notes,
                updated_at = datetime('now')
            WHERE id = @Id
        ", lead);
    }

    public void Delete(int id)
    {
        using var conn = Database.GetConnection();
        conn.Execute("DELETE FROM leads WHERE id = @id", new { id });
    }

    /// <summary>
    /// Links a lead to the company it became and moves it to the "won" status.
    /// The lead row is kept - it's the record that the cold call succeeded.
    /// </summary>
    public void MarkConverted(int leadId, int companyId, int? wonStatusId)
    {
        using var conn = Database.GetConnection();
        conn.Execute(@"
            UPDATE leads SET
                converted_company_id = @companyId,
                status_id            = COALESCE(@wonStatusId, status_id),
                updated_at           = datetime('now')
            WHERE id = @leadId
        ", new { leadId, companyId, wonStatusId });
    }
}

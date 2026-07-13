using Dapper;
using Haystack.Models;

namespace Haystack.Data;

public class ContactRepository
{
    // Shared SELECT: contact columns + owning company name + last interaction date.
    private const string SelectContact = @"
        SELECT
            c.id            AS Id,
            c.company_id    AS CompanyId,
            co.name         AS CompanyName,
            c.name          AS Name,
            c.title         AS Title,
            c.type          AS Type,
            c.status        AS Status,
            c.phone         AS Phone,
            c.email         AS Email,
            c.location      AS Location,
            c.notes         AS Notes,
            c.created_at    AS CreatedAt,
            c.updated_at    AS UpdatedAt,
            MAX(i.date)     AS LastInteractionDate
        FROM contacts c
        JOIN companies co ON co.id = c.company_id
        LEFT JOIN interactions i ON i.contact_id = c.id ";

    public IEnumerable<Contact> GetAll()
    {
        using var conn = Database.GetConnection();
        return conn.Query<Contact>(SelectContact + @"
            GROUP BY c.id
            ORDER BY c.name
        ");
    }

    public IEnumerable<Contact> GetByCompany(int companyId)
    {
        using var conn = Database.GetConnection();
        return conn.Query<Contact>(SelectContact + @"
            WHERE c.company_id = @companyId
            GROUP BY c.id
            ORDER BY c.name
        ", new { companyId });
    }

    public IEnumerable<Contact> GetStale(int thresholdDays = 60)
    {
        using var conn = Database.GetConnection();
        return conn.Query<Contact>(SelectContact + @"
            WHERE c.status = 0
            GROUP BY c.id
            HAVING LastInteractionDate IS NULL
               OR  julianday('now') - julianday(LastInteractionDate) >= @threshold
            ORDER BY LastInteractionDate ASC
        ", new { threshold = thresholdDays });
    }

    public Contact? GetById(int id)
    {
        using var conn = Database.GetConnection();
        return conn.QueryFirstOrDefault<Contact>(SelectContact + @"
            WHERE c.id = @id
            GROUP BY c.id
        ", new { id });
    }

    public int Insert(Contact contact)
    {
        using var conn = Database.GetConnection();
        return conn.ExecuteScalar<int>(@"
            INSERT INTO contacts (company_id, name, title, type, status, phone, email, location, notes, created_at, updated_at)
            VALUES (@CompanyId, @Name, @Title, @Type, @Status, @Phone, @Email, @Location, @Notes, datetime('now'), datetime('now'));
            SELECT last_insert_rowid();
        ", contact);
    }

    public void Update(Contact contact)
    {
        using var conn = Database.GetConnection();
        conn.Execute(@"
            UPDATE contacts SET
                company_id = @CompanyId,
                name       = @Name,
                title      = @Title,
                type       = @Type,
                status     = @Status,
                phone      = @Phone,
                email      = @Email,
                location   = @Location,
                notes      = @Notes,
                updated_at = datetime('now')
            WHERE id = @Id
        ", contact);
    }

    public void Delete(int id)
    {
        using var conn = Database.GetConnection();
        conn.Execute("DELETE FROM contacts WHERE id = @id", new { id });
    }

    public IEnumerable<Contact> Search(string query)
    {
        using var conn = Database.GetConnection();
        var like = $"%{query}%";
        return conn.Query<Contact>(SelectContact + @"
            WHERE c.name LIKE @like OR co.name LIKE @like OR c.location LIKE @like
            GROUP BY c.id
            ORDER BY c.name
        ", new { like });
    }
}

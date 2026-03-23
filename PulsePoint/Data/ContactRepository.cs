using Dapper;
using PulsePoint.Models;

namespace PulsePoint.Data;

public class ContactRepository
{
    public IEnumerable<Contact> GetAll()
    {
        using var conn = Database.GetConnection();
        return conn.Query<Contact>(@"
            SELECT
                c.id            AS Id,
                c.name          AS Name,
                c.company       AS Company,
                c.type          AS Type,
                c.status        AS Status,
                c.phone         AS Phone,
                c.email         AS Email,
                c.city          AS City,
                c.state         AS State,
                c.notes         AS Notes,
                c.created_at    AS CreatedAt,
                c.updated_at    AS UpdatedAt,
                MAX(i.date)     AS LastInteractionDate
            FROM contacts c
            LEFT JOIN interactions i ON i.contact_id = c.id
            GROUP BY c.id
            ORDER BY c.name
        ");
    }

    public IEnumerable<Contact> GetStale(int thresholdDays = 60)
    {
        using var conn = Database.GetConnection();
        return conn.Query<Contact>(@"
            SELECT
                c.id            AS Id,
                c.name          AS Name,
                c.company       AS Company,
                c.type          AS Type,
                c.status        AS Status,
                c.phone         AS Phone,
                c.email         AS Email,
                c.city          AS City,
                c.state         AS State,
                c.notes         AS Notes,
                c.created_at    AS CreatedAt,
                c.updated_at    AS UpdatedAt,
                MAX(i.date)     AS LastInteractionDate
            FROM contacts c
            LEFT JOIN interactions i ON i.contact_id = c.id
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
        return conn.QueryFirstOrDefault<Contact>(@"
            SELECT
                c.id            AS Id,
                c.name          AS Name,
                c.company       AS Company,
                c.type          AS Type,
                c.status        AS Status,
                c.phone         AS Phone,
                c.email         AS Email,
                c.city          AS City,
                c.state         AS State,
                c.notes         AS Notes,
                c.created_at    AS CreatedAt,
                c.updated_at    AS UpdatedAt,
                MAX(i.date)     AS LastInteractionDate
            FROM contacts c
            LEFT JOIN interactions i ON i.contact_id = c.id
            WHERE c.id = @id
            GROUP BY c.id
        ", new { id });
    }

    public int Insert(Contact contact)
    {
        using var conn = Database.GetConnection();
        return conn.ExecuteScalar<int>(@"
            INSERT INTO contacts (name, company, type, status, phone, email, city, state, notes, created_at, updated_at)
            VALUES (@Name, @Company, @Type, @Status, @Phone, @Email, @City, @State, @Notes, datetime('now'), datetime('now'));
            SELECT last_insert_rowid();
        ", contact);
    }

    public void Update(Contact contact)
    {
        using var conn = Database.GetConnection();
        conn.Execute(@"
            UPDATE contacts SET
                name       = @Name,
                company    = @Company,
                type       = @Type,
                status     = @Status,
                phone      = @Phone,
                email      = @Email,
                city       = @City,
                state      = @State,
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
        return conn.Query<Contact>(@"
            SELECT
                c.id            AS Id,
                c.name          AS Name,
                c.company       AS Company,
                c.type          AS Type,
                c.status        AS Status,
                c.phone         AS Phone,
                c.email         AS Email,
                c.city          AS City,
                c.state         AS State,
                c.notes         AS Notes,
                c.created_at    AS CreatedAt,
                c.updated_at    AS UpdatedAt,
                MAX(i.date)     AS LastInteractionDate
            FROM contacts c
            LEFT JOIN interactions i ON i.contact_id = c.id
            WHERE c.name LIKE @like OR c.company LIKE @like OR c.city LIKE @like
            GROUP BY c.id
            ORDER BY c.name
        ", new { like });
    }
}

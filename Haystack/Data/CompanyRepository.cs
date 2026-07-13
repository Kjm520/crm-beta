using Dapper;
using Haystack.Models;

namespace Haystack.Data;

public class CompanyRepository
{
    // Company columns + contact count + latest interaction across ALL its contacts.
    private const string SelectCompany = @"
        SELECT
            co.id           AS Id,
            co.name         AS Name,
            co.type         AS Type,
            co.status       AS Status,
            co.phone        AS Phone,
            co.email        AS Email,
            co.location     AS Location,
            co.notes        AS Notes,
            co.created_at   AS CreatedAt,
            co.updated_at   AS UpdatedAt,
            COUNT(DISTINCT c.id) AS ContactCount,
            MAX(i.date)          AS LastInteractionDate
        FROM companies co
        LEFT JOIN contacts c     ON c.company_id = co.id
        LEFT JOIN interactions i ON i.contact_id = c.id ";

    public IEnumerable<Company> GetAll()
    {
        using var conn = Database.GetConnection();
        return conn.Query<Company>(SelectCompany + @"
            GROUP BY co.id
            ORDER BY co.name
        ");
    }

    public Company? GetById(int id)
    {
        using var conn = Database.GetConnection();
        return conn.QueryFirstOrDefault<Company>(SelectCompany + @"
            WHERE co.id = @id
            GROUP BY co.id
        ", new { id });
    }

    public int Insert(Company company)
    {
        using var conn = Database.GetConnection();
        return conn.ExecuteScalar<int>(@"
            INSERT INTO companies (name, type, status, phone, email, location, notes, created_at, updated_at)
            VALUES (@Name, @Type, @Status, @Phone, @Email, @Location, @Notes, datetime('now'), datetime('now'));
            SELECT last_insert_rowid();
        ", company);
    }

    public void Update(Company company)
    {
        using var conn = Database.GetConnection();
        conn.Execute(@"
            UPDATE companies SET
                name       = @Name,
                type       = @Type,
                status     = @Status,
                phone      = @Phone,
                email      = @Email,
                location   = @Location,
                notes      = @Notes,
                updated_at = datetime('now')
            WHERE id = @Id
        ", company);
    }

    public void Delete(int id)
    {
        // Contacts (and their interactions) cascade-delete via the FK.
        using var conn = Database.GetConnection();
        conn.Execute("DELETE FROM companies WHERE id = @id", new { id });
    }

    public IEnumerable<Company> Search(string query)
    {
        using var conn = Database.GetConnection();
        var like = $"%{query}%";
        return conn.Query<Company>(SelectCompany + @"
            WHERE co.name LIKE @like OR co.location LIKE @like
            GROUP BY co.id
            ORDER BY co.name
        ", new { like });
    }
}

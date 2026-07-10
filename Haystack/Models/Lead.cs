namespace Haystack.Models;

/// <summary>
/// A lead is a flat, standalone record - deliberately NOT linked to companies or
/// contacts. Leads are stored in bulk for later reference, not run through a
/// sales pipeline.
/// </summary>
public class Lead
{
    public int Id { get; set; }

    // Either Name or Company is required (enforced in validation, not the schema).
    public string? Name { get; set; }
    public string? Company { get; set; }

    public string? Phone { get; set; }
    public string? Email { get; set; }

    // User-editable status (see LeadStatus). Null when no status is set, or when
    // the assigned status was deleted.
    public int? StatusId { get; set; }

    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// The company this lead became, once converted. Null while the lead is still
    /// cold. The lead record is kept after conversion as proof of the cold call.
    /// </summary>
    public int? ConvertedCompanyId { get; set; }

    public bool IsConverted => ConvertedCompanyId.HasValue;

    // Populated by join query for display.
    public string? StatusName { get; set; }

    /// <summary>What to show in a list when a lead has no name (falls back to company).</summary>
    public string DisplayName =>
        !string.IsNullOrWhiteSpace(Name) ? Name!
        : !string.IsNullOrWhiteSpace(Company) ? Company!
        : "(untitled lead)";
}

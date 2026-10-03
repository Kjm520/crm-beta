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

    // Populated by join query: whether the assigned status is flagged closed
    // (finished · won/lost/dead). Statusless leads count as open.
    public bool StatusIsClosed { get; set; }

    /// <summary>
    /// How to label this lead (falls back to the company when it has no name).
    /// Empty for a lead that has neither · i.e. a new, not-yet-filled-in one, whose
    /// modal title should read blank like Contact's and Company's do. Validation
    /// requires Name or Company, so a saved lead always labels as something.
    /// </summary>
    public string DisplayName =>
        !string.IsNullOrWhiteSpace(Name) ? Name!
        : !string.IsNullOrWhiteSpace(Company) ? Company!
        : string.Empty;
}

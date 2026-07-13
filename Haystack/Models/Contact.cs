namespace Haystack.Models;

public class Contact
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Title { get; set; }
    public ContactType Type { get; set; } = ContactType.Customer;
    public ContactStatus Status { get; set; } = ContactStatus.Active;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Location { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Populated by join query for display (the owning company's name).
    public string? CompanyName { get; set; }

    // Computed - populated by query, not stored
    public DateTime? LastInteractionDate { get; set; }
    public int DaysSinceLastInteraction => LastInteractionDate.HasValue
        ? (int)(DateTime.Today - LastInteractionDate.Value.Date).TotalDays
        : int.MaxValue;

    // Display-friendly version for the UI: "-" when there's no interaction yet.
    public string DaysSinceLastInteractionDisplay => LastInteractionDate.HasValue
        ? DaysSinceLastInteraction.ToString()
        : "-";
}

public enum ContactType
{
    Customer,
    Prospect,
    Carrier
}

public enum ContactStatus
{
    Active,
    Inactive
}

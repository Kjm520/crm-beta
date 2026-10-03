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

    /// <summary>
    /// Relationship-monitoring cadence: this contact should see some activity at
    /// least every this-many days. Null = not monitored. Drives
    /// <see cref="IsOverdue"/> and the alert bell.
    /// </summary>
    public int? ContactEveryDays { get; set; }

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

    // Display-friendly cadence for the MAINTENANCE column: "-" when not monitored.
    public string ContactEveryDaysDisplay => ContactEveryDays is { } days
        ? days == 1 ? "1 day" : $"{days} days"
        : "-";

    // Sort member for the MAINTENANCE column: unmonitored lands last ascending.
    public int ContactEveryDaysSort => ContactEveryDays ?? int.MaxValue;

    /// <summary>
    /// Days since the last activity, counting from creation when nothing was
    /// ever logged · a new record gets its full cadence window before alarming,
    /// but can't hide forever.
    /// </summary>
    public int EffectiveDaysSince => LastInteractionDate.HasValue
        ? DaysSinceLastInteraction
        : (int)(DateTime.Today - CreatedAt.Date).TotalDays;

    /// <summary>True when a cadence is set and its window has lapsed.</summary>
    public bool IsOverdue => ContactEveryDays is { } cadence && EffectiveDaysSince > cadence;
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

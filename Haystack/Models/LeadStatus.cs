namespace Haystack.Models;

/// <summary>
/// A user-editable lead status. Stored as data (not a C# enum) so statuses can be
/// added, renamed, and removed from the app without a code change.
/// </summary>
public class LeadStatus
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    /// <summary>
    /// Marks the status a lead is moved to when converted into a company. Tracked as
    /// a flag rather than by name so the status can be renamed freely in Settings.
    /// </summary>
    public bool IsWon { get; set; }

    // Populated by query when we need to warn before deleting a status in use.
    public int LeadCount { get; set; }
}

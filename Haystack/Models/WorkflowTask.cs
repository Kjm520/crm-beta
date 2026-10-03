namespace Haystack.Models;

/// <summary>
/// One item in the Workflow queue: a piece of work that needs doing · typed by
/// hand, or materialized automatically by a monitor (see Alerts/MonitorEngine).
/// New tasks append to the bottom and the queue reads oldest-first, so the
/// longest-outstanding work always sits at the top. Completing sets
/// <see cref="DoneAt"/> rather than deleting the row.
/// </summary>
public class WorkflowTask
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? DoneAt { get; set; }

    /// <summary>
    /// The condition that created this task (e.g. "cadence:company:12") · the
    /// monitor's dedup key and how it finds the task again to auto-resolve it.
    /// Null = added by hand.
    /// </summary>
    public string? Source { get; set; }

    // The record this task is about, for click-through. Null = plain text task.
    public RecordKind? TargetKind { get; set; }
    public int? TargetId { get; set; }

    public bool HasTarget => TargetKind is not null && TargetId is not null;

    public int AgeDays => (int)(DateTime.Today - CreatedAt.Date).TotalDays;

    public string AgeDisplay => AgeDays switch
    {
        <= 0 => "today",
        1 => "1 day",
        var days => $"{days} days",
    };
}

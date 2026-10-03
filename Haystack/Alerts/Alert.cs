using Haystack.Models;

namespace Haystack.Alerts;

/// <summary>
/// One item in the title-bar alert bell: which group it renders under, what to
/// show, and which record to open when clicked. The bell is a LENS, not a
/// second list · its items are derived on every refresh (from the Workflow
/// queue's open monitor tasks, and any future non-task rules), never stored,
/// so they can't go stale or drift from the source of truth.
/// </summary>
public record Alert(
    string Group,
    string Title,
    string Detail,
    RecordKind Target,
    int TargetId);

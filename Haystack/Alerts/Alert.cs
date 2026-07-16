namespace Haystack.Alerts;

/// <summary>What a clicked alert opens.</summary>
public enum AlertTarget
{
    Company,
    Contact,
}

/// <summary>
/// One item in the title-bar alert bell: which group it renders under, what to
/// show, and which record to open when clicked. Alerts are DERIVED — recomputed
/// from live data on every refresh, never stored — so they can't go stale,
/// need no dismissal state, and need no migrations as rules evolve.
/// </summary>
public record Alert(
    string Group,
    string Title,
    string Detail,
    AlertTarget Target,
    int TargetId);

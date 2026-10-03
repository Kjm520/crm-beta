using Haystack.Data;

namespace Haystack.Alerts;

/// <summary>One entry in the monitor registry.</summary>
public record Monitor(string Id, string Name, string Description, bool DefaultEnabled);

/// <summary>
/// The registry of monitors · rules that watch live data and materialize tasks
/// into the Workflow queue automatically (see MonitorEngine for the mechanics).
/// Consent lives at rule configuration: the user set the rule up, so its output
/// needs no per-item approval.
///
/// Two levels of enablement, by design:
///  - per record: the rule's own subject config (e.g. the cadence field; empty
///    = that record isn't monitored) · a fact about the record, edited on it;
///  - global: the toggle here (Settings → MONITORS) · a master switch per rule
///    class, e.g. for vacations or a rule that proves noisy.
///
/// Adding a monitor = one entry here + one Run method in MonitorEngine; the
/// Settings card and enablement gating come free.
/// </summary>
public static class Monitors
{
    public const string Cadence = "cadence";

    public static readonly Monitor[] All =
    [
        new(Cadence, "Contact cadence",
            "Adds a task to the Workflow queue when a company or contact goes past its \"contact every N days\" interval, and clears it again once activity is logged.",
            DefaultEnabled: true),
    ];

    public static string EnabledKey(string id) => "monitor." + id;

    public static bool IsEnabled(string id)
    {
        var monitor = All.First(m => m.Id == id);
        var saved = new AppSettingsRepository().Get(EnabledKey(id));
        return saved is null ? monitor.DefaultEnabled : saved == "1";
    }

    public static void SetEnabled(string id, bool enabled)
    {
        new AppSettingsRepository().Set(EnabledKey(id), enabled ? "1" : "0");

        // A disabled monitor can't resolve its own tasks anymore, so they'd sit
        // open forever · retire them with it.
        if (!enabled)
            MonitorEngine.Retire(id);
    }
}

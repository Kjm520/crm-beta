using Haystack.Data;
using Haystack.Models;

namespace Haystack.Alerts;

/// <summary>
/// Evaluates every enabled monitor against live data and syncs the Workflow
/// queue to match. Runs at the same moments the alert bell refreshes (startup,
/// any modal closing, the rollover timer). For each monitor:
///
///  - a condition that's firing with no task for it yet ⇒ CREATE one (stamped
///    with a source key like "cadence:company:12" · one condition, one task,
///    never one per refresh);
///  - a condition that stopped firing while its task is open ⇒ RESOLVE the task
///    (activity logged, cadence removed, record deleted · the queue always
///    reflects reality);
///  - a task the user ticked by hand while the condition still fires ⇒ SUPPRESS
///    re-creation for that episode: nothing new until the condition genuinely
///    clears and later fires again. Detected with no extra state, by comparing
///    the completion time against the episode's anchor (the record's last
///    activity, or its creation when none was ever logged).
/// </summary>
public static class MonitorEngine
{
    public static void Run()
    {
        if (Monitors.IsEnabled(Monitors.Cadence))
            RunCadence();
    }

    /// <summary>Retires a disabled monitor's open tasks.</summary>
    public static void Retire(string monitorId)
        => new WorkflowTaskRepository().CompleteBySourcePrefix(monitorId + ":");

    private static void RunCadence()
    {
        var tasks = new WorkflowTaskRepository();
        var open = tasks.GetOpenSources();
        var lastDone = tasks.GetLatestDoneBySource();

        // Everything the cadence rule is firing on right now. The anchor marks
        // the current episode: activity after a manual tick starts a new one.
        var firing = new Dictionary<string, (string Text, RecordKind Kind, int Id, DateTime Anchor)>();

        foreach (var company in new CompanyRepository().GetAll().Where(c => c.IsOverdue))
            firing[$"{Monitors.Cadence}:company:{company.Id}"] = (
                $"Contact {company.Name} · every {company.ContactEveryDays} days",
                RecordKind.Company, company.Id,
                company.LastInteractionDate ?? company.CreatedAt);

        foreach (var contact in new ContactRepository().GetAll().Where(c => c.IsOverdue))
            firing[$"{Monitors.Cadence}:contact:{contact.Id}"] = (
                $"Contact {contact.Name} ({contact.CompanyName}) · every {contact.ContactEveryDays} days",
                RecordKind.Contact, contact.Id,
                contact.LastInteractionDate ?? contact.CreatedAt);

        // Create newly-firing conditions' tasks (unless this episode was
        // manually dismissed). Timestamps are compared at day-ish granularity:
        // done_at is stored UTC while activity dates are user-picked local
        // dates · a few hours of skew is fine for a rule that thinks in days.
        foreach (var (key, condition) in firing)
        {
            if (open.ContainsKey(key)) continue;
            if (lastDone.TryGetValue(key, out var dismissed) && dismissed >= condition.Anchor) continue;

            tasks.InsertMonitorTask(condition.Text, key, condition.Kind, condition.Id);
        }

        // Resolve tasks whose condition stopped firing.
        foreach (var (key, taskId) in open)
            if (key.StartsWith($"{Monitors.Cadence}:") && !firing.ContainsKey(key))
                tasks.Complete(taskId);
    }
}

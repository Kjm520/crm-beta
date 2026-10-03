using Haystack.Data;

namespace Haystack.Alerts;

/// <summary>
/// The single compose-point for everything the alert bell shows. Each source is
/// one private method returning its alerts; <see cref="GatherAll"/> concatenates
/// them and the bell renders whatever comes back, grouped by Alert.Group.
/// MonitorEngine has already synced the Workflow queue by the time this runs
/// (see MainWindow.RefreshAlerts), so the bell simply mirrors the queue's open
/// monitor tasks · one source of truth. Future non-task rules (e.g. backup
/// reminders) are one method and one line here.
/// </summary>
public static class AlertCenter
{
    public static List<Alert> GatherAll() =>
        [.. MonitorTasks()];

    /// <summary>The open monitor-created tasks, as bell items with click-through.</summary>
    private static IEnumerable<Alert> MonitorTasks()
    {
        foreach (var task in new WorkflowTaskRepository().GetOpenMonitorTasks())
        {
            if (task.TargetKind is not { } kind || task.TargetId is not { } id) continue;

            yield return new Alert(
                "NEEDS CONTACT",
                task.Text,
                task.AgeDays <= 0 ? "in the queue since today" : $"in the queue {task.AgeDisplay}",
                kind, id);
        }
    }
}

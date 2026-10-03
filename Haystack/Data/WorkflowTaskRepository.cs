using Dapper;
using Haystack.Models;

namespace Haystack.Data;

public class WorkflowTaskRepository
{
    private const string SelectTask = @"
        SELECT
            id          AS Id,
            text        AS Text,
            created_at  AS CreatedAt,
            done_at     AS DoneAt,
            source      AS Source,
            target_kind AS TargetKind,
            target_id   AS TargetId
        FROM workflow_tasks ";

    /// <summary>The open queue, oldest first (id breaks same-second ties).</summary>
    public IEnumerable<WorkflowTask> GetOpen()
    {
        using var conn = Database.GetConnection();
        return conn.Query<WorkflowTask>(SelectTask + @"
            WHERE done_at IS NULL
            ORDER BY created_at, id
        ");
    }

    /// <summary>Open monitor-created tasks only · what the alert bell lists.</summary>
    public IEnumerable<WorkflowTask> GetOpenMonitorTasks()
    {
        using var conn = Database.GetConnection();
        return conn.Query<WorkflowTask>(SelectTask + @"
            WHERE done_at IS NULL AND source IS NOT NULL
            ORDER BY created_at, id
        ");
    }

    /// <summary>Appends a hand-typed task and returns it as stored.</summary>
    public WorkflowTask Insert(string text)
    {
        using var conn = Database.GetConnection();
        return conn.QueryFirst<WorkflowTask>(@"
            INSERT INTO workflow_tasks (text, created_at) VALUES (@text, datetime('now'));
        " + SelectTask + "WHERE id = last_insert_rowid();", new { text });
    }

    /// <summary>Appends a monitor-created task stamped with its origin and subject.</summary>
    public void InsertMonitorTask(string text, string source, RecordKind targetKind, int targetId)
    {
        using var conn = Database.GetConnection();
        conn.Execute(@"
            INSERT INTO workflow_tasks (text, created_at, source, target_kind, target_id)
            VALUES (@text, datetime('now'), @source, @targetKind, @targetId)
        ", new { text, source, targetKind, targetId });
    }

    /// <summary>Marks a task done. The row is kept (see the v12 migration note).</summary>
    public void Complete(int id)
    {
        using var conn = Database.GetConnection();
        conn.Execute("UPDATE workflow_tasks SET done_at = datetime('now') WHERE id = @id", new { id });
    }

    // --- Monitor support (see Alerts/MonitorEngine) --------------------------

    /// <summary>Open monitor tasks as source → task id, for dedup and auto-resolve.</summary>
    public Dictionary<string, int> GetOpenSources()
    {
        using var conn = Database.GetConnection();
        return conn.Query<(string Source, int Id)>(@"
            SELECT source, id FROM workflow_tasks
            WHERE done_at IS NULL AND source IS NOT NULL
        ").ToDictionary(row => row.Source, row => row.Id);
    }

    /// <summary>
    /// Newest completion time per source · how a monitor knows a manual tick
    /// already dismissed the current episode of a condition.
    /// </summary>
    public Dictionary<string, DateTime> GetLatestDoneBySource()
    {
        using var conn = Database.GetConnection();
        return conn.Query<(string Source, DateTime Done)>(@"
            SELECT source, MAX(done_at) FROM workflow_tasks
            WHERE done_at IS NOT NULL AND source IS NOT NULL
            GROUP BY source
        ").ToDictionary(row => row.Source, row => row.Done);
    }

    /// <summary>
    /// Retires every open task a monitor created (used when the monitor is
    /// disabled · with its resolver switched off, they'd be stuck open forever).
    /// </summary>
    public void CompleteBySourcePrefix(string prefix)
    {
        using var conn = Database.GetConnection();
        conn.Execute(@"
            UPDATE workflow_tasks SET done_at = datetime('now')
            WHERE done_at IS NULL AND source LIKE @pattern
        ", new { pattern = prefix + "%" });
    }
}

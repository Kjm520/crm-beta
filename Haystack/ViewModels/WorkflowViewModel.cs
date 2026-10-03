using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Haystack.Data;
using Haystack.Models;
using Haystack.ViewModels.Base;

namespace Haystack.ViewModels;

/// <summary>
/// The Workflow queue: free-text tasks worked oldest-first. New tasks append to
/// the bottom of the collection, so the longest-outstanding item is always the
/// one staring back from the top.
/// </summary>
public partial class WorkflowViewModel : ViewModelBase
{
    private readonly WorkflowTaskRepository _repo = new();

    [ObservableProperty]
    private ObservableCollection<WorkflowTask> _tasks = [];

    [ObservableProperty]
    private string _newTaskText = string.Empty;

    [RelayCommand]
    public void Load()
    {
        Tasks.Clear();
        foreach (var task in _repo.GetOpen()) Tasks.Add(task);
    }

    /// <summary>Appends the typed task to the bottom of the queue. Blank input is ignored.</summary>
    public void AddTask()
    {
        var text = NewTaskText.Trim();
        if (text.Length == 0) return;

        Tasks.Add(_repo.Insert(text));
        NewTaskText = string.Empty;
    }

    public void Complete(WorkflowTask task)
    {
        _repo.Complete(task.Id);
        Tasks.Remove(task);
    }
}

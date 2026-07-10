using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Haystack.Data;
using Haystack.Models;
using Haystack.ViewModels.Base;

namespace Haystack.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly LeadStatusRepository _statusRepo = new();

    [ObservableProperty]
    private ObservableCollection<LeadStatus> _leadStatuses = [];

    [ObservableProperty]
    private string _newStatusName = string.Empty;

    public SettingsViewModel() => LoadStatuses();

    public void LoadStatuses()
    {
        LeadStatuses = new ObservableCollection<LeadStatus>(_statusRepo.GetAll());
    }

    /// <summary>Adds a status. Returns an error message, or null on success.</summary>
    public string? AddStatus()
    {
        var name = NewStatusName.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return "Enter a status name.";
        if (_statusRepo.NameExists(name))
            return $"A status named \"{name}\" already exists.";

        var nextOrder = LeadStatuses.Count == 0 ? 0 : LeadStatuses.Max(s => s.SortOrder) + 1;
        _statusRepo.Insert(new LeadStatus { Name = name, SortOrder = nextOrder });

        NewStatusName = string.Empty;
        LoadStatuses();
        return null;
    }

    /// <summary>Renames a status. Returns an error message, or null on success.</summary>
    public string? RenameStatus(LeadStatus status, string newName)
    {
        var name = newName.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return "Enter a status name.";
        if (_statusRepo.NameExists(name, excludeId: status.Id))
            return $"A status named \"{name}\" already exists.";

        status.Name = name;
        _statusRepo.Update(status);
        LoadStatuses();
        return null;
    }

    [RelayCommand]
    public void DeleteStatus(LeadStatus status)
    {
        _statusRepo.Delete(status.Id);
        LoadStatuses();
    }
}

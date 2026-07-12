using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Haystack.Data;
using Haystack.Models;
using Haystack.Navigation;
using Haystack.ViewModels.Base;

namespace Haystack.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly LeadStatusRepository _statusRepo = new();
    private readonly AppSettingsRepository _appSettings = new();

    [ObservableProperty]
    private ObservableCollection<LeadStatus> _leadStatuses = [];

    [ObservableProperty]
    private string _newStatusName = string.Empty;

    // Left-panel tabs, in their current order, for the reorder UI.
    [ObservableProperty]
    private ObservableCollection<NavItem> _navItems = [];

    public SettingsViewModel()
    {
        LoadStatuses();
        LoadNav();
    }

    public void LoadStatuses()
    {
        LeadStatuses = new ObservableCollection<LeadStatus>(_statusRepo.GetAll());
    }

    public void LoadNav()
    {
        NavItems = new ObservableCollection<NavItem>(
            NavRegistry.InSavedOrder(_appSettings.Get(NavRegistry.OrderSettingKey)));
    }

    public void MoveNavUp(NavItem item)
    {
        var i = NavItems.IndexOf(item);
        if (i > 0) { NavItems.Move(i, i - 1); SaveNavOrder(); }
    }

    public void MoveNavDown(NavItem item)
    {
        var i = NavItems.IndexOf(item);
        if (i >= 0 && i < NavItems.Count - 1) { NavItems.Move(i, i + 1); SaveNavOrder(); }
    }

    private void SaveNavOrder()
        => _appSettings.Set(NavRegistry.OrderSettingKey, string.Join(",", NavItems.Select(n => n.Key)));

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

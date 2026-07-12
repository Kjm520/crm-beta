using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Haystack.Data;
using Haystack.Models;
using Haystack.Navigation;
using Haystack.Theming;
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

    // Appearance
    [ObservableProperty]
    private bool _isDarkMode;

    public SettingsViewModel()
    {
        // Seed the toggle from the live theme without re-triggering a swap.
        _isDarkMode = ThemeManager.IsDark;

        LoadStatuses();
        LoadNav();
    }

    partial void OnIsDarkModeChanged(bool value)
        => ThemeManager.Apply(value ? ThemeManager.Dark : ThemeManager.Light);

    public void LoadStatuses()
    {
        LeadStatuses = new ObservableCollection<LeadStatus>(_statusRepo.GetAll());
    }

    public void LoadNav()
    {
        NavItems = new ObservableCollection<NavItem>(
            NavRegistry.InSavedOrder(_appSettings.Get(NavRegistry.OrderSettingKey)));
    }

    /// <summary>Moves <paramref name="item"/> to the position of <paramref name="target"/>
    /// (drag-and-drop reorder) and persists the new order.</summary>
    public void MoveNav(NavItem item, NavItem target)
    {
        var from = NavItems.IndexOf(item);
        var to = NavItems.IndexOf(target);
        if (from < 0 || to < 0 || from == to) return;

        NavItems.Move(from, to);
        SaveNavOrder();
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

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

    /// <summary>
    /// Moves a nav tab to a new index. Called repeatedly while a drag is in flight
    /// (once per row crossed), so it deliberately does NOT persist · the order is
    /// written once the drag settles, via <see cref="CommitNavOrder"/>.
    /// </summary>
    public void MoveNav(int from, int to)
    {
        if (from == to) return;
        if (from < 0 || from >= NavItems.Count) return;
        if (to   < 0 || to   >= NavItems.Count) return;

        NavItems.Move(from, to);
    }

    /// <summary>Persists the current tab order (once, when a reorder drag finishes).</summary>
    public void CommitNavOrder() => SaveNavOrder();

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

    /// <summary>
    /// Flags whether a status means "finished" (won/lost/dead). Leads on a closed
    /// status are hidden from the Leads list unless its "Show closed" toggle is on.
    /// </summary>
    public void SetStatusClosed(LeadStatus status, bool closed)
    {
        status.IsClosed = closed;
        _statusRepo.Update(status);
    }

    [RelayCommand]
    public void DeleteStatus(LeadStatus status)
    {
        _statusRepo.Delete(status.Id);
        LoadStatuses();
    }
}

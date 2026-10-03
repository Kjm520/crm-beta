using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Haystack.Data;
using Haystack.Models;
using Haystack.ViewModels.Base;

namespace Haystack.ViewModels;

public partial class LeadsViewModel : ViewModelBase
{
    private const string ShowClosedKey = "leads.showClosed";

    private readonly LeadRepository _repo = new();
    private readonly AppSettingsRepository _appSettings = new();

    [ObservableProperty]
    private ObservableCollection<Lead> _leads = [];

    [ObservableProperty]
    private string _searchText = string.Empty;

    /// <summary>
    /// Whether leads on a closed status (won/lost/dead · see LeadStatus.IsClosed)
    /// are shown. Off by default: a closed lead needs nothing further, so it's
    /// noise in the working list. Persisted so the choice survives restarts.
    /// </summary>
    [ObservableProperty]
    private bool _showClosed;

    public LeadsViewModel()
    {
        // Seed from settings via the backing field so restoring the saved value
        // doesn't re-save it or trigger a search before the first Load.
        _showClosed = _appSettings.Get(ShowClosedKey) == "1";
    }

    partial void OnSearchTextChanged(string value) => Search();

    partial void OnShowClosedChanged(bool value)
    {
        _appSettings.Set(ShowClosedKey, value ? "1" : "0");
        Search();
    }

    // Load defers to Search so a reload (e.g. after a detail modal closes) keeps
    // respecting whatever is typed in the search box.
    [RelayCommand]
    public void Load() => Search();

    [RelayCommand]
    public void Search()
    {
        var results = string.IsNullOrWhiteSpace(SearchText)
            ? _repo.GetAll()
            : _repo.Search(SearchText);

        if (!ShowClosed)
            results = results.Where(l => !l.StatusIsClosed);

        Refill(results);
    }

    /// <summary>
    /// Refills the list in place · never replaces the collection. The table
    /// sorts through the collection's view (see TableHeader), and swapping in a
    /// new collection would throw away the user's sort on every keystroke/reload.
    /// </summary>
    private void Refill(IEnumerable<Lead> rows)
    {
        Leads.Clear();
        foreach (var row in rows) Leads.Add(row);
    }

    [RelayCommand]
    public void Delete(Lead lead)
    {
        _repo.Delete(lead.Id);
        Leads.Remove(lead);
    }
}

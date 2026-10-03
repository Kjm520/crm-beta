using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Haystack.Data;
using Haystack.Models;
using Haystack.ViewModels.Base;

namespace Haystack.ViewModels;

public partial class ContactsViewModel : ViewModelBase
{
    private readonly ContactRepository _repo = new();

    [ObservableProperty]
    private ObservableCollection<Contact> _contacts = [];

    [ObservableProperty]
    private Contact? _selectedContact;

    [ObservableProperty]
    private string _searchText = string.Empty;

    partial void OnSearchTextChanged(string value)
    {
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

        Refill(results);
    }

    /// <summary>
    /// Refills the list in place · never replaces the collection. The table
    /// sorts through the collection's view (see TableHeader), and swapping in a
    /// new collection would throw away the user's sort on every keystroke/reload.
    /// </summary>
    private void Refill(IEnumerable<Contact> rows)
    {
        Contacts.Clear();
        foreach (var row in rows) Contacts.Add(row);
    }

    [RelayCommand]
    public void Delete(Contact contact)
    {
        _repo.Delete(contact.Id);
        Contacts.Remove(contact);
    }
}

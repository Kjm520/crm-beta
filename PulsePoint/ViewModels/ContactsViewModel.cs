using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PulsePoint.Data;
using PulsePoint.Models;
using PulsePoint.ViewModels.Base;

namespace PulsePoint.ViewModels;

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

    [RelayCommand]
    public void Load()
    {
        Contacts = new ObservableCollection<Contact>(_repo.GetAll());
    }

    [RelayCommand]
    public void Search()
    {
        var results = string.IsNullOrWhiteSpace(SearchText)
            ? _repo.GetAll()
            : _repo.Search(SearchText);

        Contacts = new ObservableCollection<Contact>(results);
    }

    [RelayCommand]
    public void Delete(Contact contact)
    {
        _repo.Delete(contact.Id);
        Contacts.Remove(contact);
    }
}

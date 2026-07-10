using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Haystack.Data;
using Haystack.Models;
using Haystack.ViewModels.Base;

namespace Haystack.ViewModels;

public partial class LeadsViewModel : ViewModelBase
{
    private readonly LeadRepository _repo = new();

    [ObservableProperty]
    private ObservableCollection<Lead> _leads = [];

    [ObservableProperty]
    private string _searchText = string.Empty;

    partial void OnSearchTextChanged(string value) => Search();

    [RelayCommand]
    public void Load()
    {
        Leads = new ObservableCollection<Lead>(_repo.GetAll());
    }

    [RelayCommand]
    public void Search()
    {
        var results = string.IsNullOrWhiteSpace(SearchText)
            ? _repo.GetAll()
            : _repo.Search(SearchText);

        Leads = new ObservableCollection<Lead>(results);
    }

    [RelayCommand]
    public void Delete(Lead lead)
    {
        _repo.Delete(lead.Id);
        Leads.Remove(lead);
    }
}

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Haystack.Data;
using Haystack.Models;
using Haystack.ViewModels.Base;

namespace Haystack.ViewModels;

/// <summary>
/// The Companies list. Deliberately has no search: this is the relations side
/// of the app — the working set of actual customers — not the lead pipeline,
/// and at that size scanning a sortable table beats typing a query.
/// </summary>
public partial class CompaniesViewModel : ViewModelBase
{
    private readonly CompanyRepository _repo = new();

    [ObservableProperty]
    private ObservableCollection<Company> _companies = [];

    /// <summary>
    /// Refills the list in place — never replaces the collection. The table
    /// sorts through the collection's view (see TableHeader), and swapping in a
    /// new collection would throw away the user's sort on every reload.
    /// </summary>
    [RelayCommand]
    public void Load()
    {
        Companies.Clear();
        foreach (var company in _repo.GetAll()) Companies.Add(company);
    }
}

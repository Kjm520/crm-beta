using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Haystack.Data;
using Haystack.Models;
using Haystack.ViewModels.Base;

namespace Haystack.ViewModels;

public partial class CompaniesViewModel : ViewModelBase
{
    private readonly CompanyRepository _repo = new();

    [ObservableProperty]
    private ObservableCollection<Company> _companies = [];

    [ObservableProperty]
    private string _searchText = string.Empty;

    partial void OnSearchTextChanged(string value) => Search();

    [RelayCommand]
    public void Load()
    {
        Companies = new ObservableCollection<Company>(_repo.GetAll());
    }

    [RelayCommand]
    public void Search()
    {
        var results = string.IsNullOrWhiteSpace(SearchText)
            ? _repo.GetAll()
            : _repo.Search(SearchText);

        Companies = new ObservableCollection<Company>(results);
    }
}

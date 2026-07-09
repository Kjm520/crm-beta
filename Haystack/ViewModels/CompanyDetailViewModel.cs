using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Haystack.Data;
using Haystack.Models;
using Haystack.ViewModels.Base;

namespace Haystack.ViewModels;

public partial class CompanyDetailViewModel : ViewModelBase
{
    private readonly CompanyRepository _companyRepo = new();
    private readonly ContactRepository _contactRepo = new();
    private readonly InteractionRepository _interactionRepo = new();

    public ContactType[] ContactTypes { get; set; } = Enum.GetValues<ContactType>();
    public ContactStatus[] ContactStatuses { get; set; } = Enum.GetValues<ContactStatus>();

    [ObservableProperty]
    private Company _company = new();

    // The company's contacts and its rolled-up activity (across all those contacts).
    [ObservableProperty]
    private ObservableCollection<Contact> _contacts = [];

    [ObservableProperty]
    private ObservableCollection<Interaction> _activity = [];

    public void LoadCompany(int companyId)
    {
        var c = _companyRepo.GetById(companyId);
        if (c is not null)
        {
            Company = c;
            RefreshChildren(companyId);
        }
    }

    /// <summary>Re-load the contacts and rolled-up activity (e.g. after adding a contact).</summary>
    public void RefreshChildren(int companyId)
    {
        Contacts = new ObservableCollection<Contact>(_contactRepo.GetByCompany(companyId));
        Activity = new ObservableCollection<Interaction>(_interactionRepo.GetByCompany(companyId));
    }

    private bool CanSave() => !string.IsNullOrWhiteSpace(Company.Name);

    [RelayCommand(CanExecute = nameof(CanSave))]
    public void SaveCompany()
    {
        Company.UpdatedAt = DateTime.UtcNow;
        if (Company.Id == 0)
            Company.Id = _companyRepo.Insert(Company);
        else
            _companyRepo.Update(Company);
    }
}

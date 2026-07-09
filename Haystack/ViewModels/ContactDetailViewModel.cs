using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Haystack.Data;
using Haystack.Models;
using Haystack.ViewModels.Base;

namespace Haystack.ViewModels;

public partial class ContactDetailViewModel : ViewModelBase
{
    private readonly ContactRepository _contactRepo = new();
    private readonly InteractionRepository _interactionRepo = new();
    private readonly CompanyRepository _companyRepo = new();

    public ContactType[] ContactTypes { get; set; } = Enum.GetValues<ContactType>();
    public ContactStatus[] ContactStatuses { get; set; } = Enum.GetValues<ContactStatus>();

    // Companies available to assign this contact to (required).
    [ObservableProperty]
    private ObservableCollection<Company> _companies = [];

    [ObservableProperty]
    private Company? _selectedCompany;

    [ObservableProperty]
    private Contact _contact = new();

    // Read-only history of this contact's interactions. Logging new interactions
    // now lives in the Activity tab; this list is view/delete only.
    [ObservableProperty]
    private ObservableCollection<Interaction> _interactions = [];

    public ContactDetailViewModel()
    {
        Companies = new ObservableCollection<Company>(_companyRepo.GetAll());
    }

    /// <summary>Pre-select a company (used when adding a contact from a company's page).</summary>
    public void PreselectCompany(int companyId)
    {
        SelectedCompany = Companies.FirstOrDefault(c => c.Id == companyId);
    }

    public void LoadContact(int contactId)
    {
        var c = _contactRepo.GetById(contactId);
        if (c is not null)
        {
            Contact = c;
            SelectedCompany = Companies.FirstOrDefault(co => co.Id == c.CompanyId);
            Interactions = new ObservableCollection<Interaction>(
                _interactionRepo.GetByContact(contactId));
        }
    }

    // Save is only valid once a company is chosen (company is required).
    private bool CanSave() => SelectedCompany is not null && !string.IsNullOrWhiteSpace(Contact.Name);

    partial void OnSelectedCompanyChanged(Company? value) => SaveContactCommand.NotifyCanExecuteChanged();

    [RelayCommand(CanExecute = nameof(CanSave))]
    public void SaveContact()
    {
        Contact.CompanyId = SelectedCompany!.Id;
        Contact.UpdatedAt = DateTime.UtcNow;
        if (Contact.Id == 0)
            Contact.Id = _contactRepo.Insert(Contact);
        else
            _contactRepo.Update(Contact);
    }

    [RelayCommand]
    public void DeleteInteraction(Interaction interaction)
    {
        _interactionRepo.Delete(interaction.Id);
        Interactions.Remove(interaction);
    }
}

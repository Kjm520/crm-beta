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

    public ContactType[] ContactTypes { get; set; } = Enum.GetValues<ContactType>();
    public ContactStatus[] ContactStatuses { get; set; } = Enum.GetValues<ContactStatus>();

    [ObservableProperty]
    private Contact _contact = new();

    // Read-only history of this contact's interactions. Logging new interactions
    // now lives in the Activity tab; this list is view/delete only.
    [ObservableProperty]
    private ObservableCollection<Interaction> _interactions = [];

    public void LoadContact(int contactId)
    {
        var c = _contactRepo.GetById(contactId);
        if (c is not null)
        {
            Contact = c;
            Interactions = new ObservableCollection<Interaction>(
                _interactionRepo.GetByContact(contactId));
        }
    }

    [RelayCommand]
    public void SaveContact()
    {
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

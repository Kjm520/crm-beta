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
    public InteractionType[] InteractionTypes { get; set; } = Enum.GetValues<InteractionType>();

    [ObservableProperty]
    private Contact _contact = new();

    [ObservableProperty]
    private ObservableCollection<Interaction> _interactions = [];

    // New interaction form fields
    [ObservableProperty]
    private InteractionType _newInteractionType = InteractionType.Call;

    [ObservableProperty]
    private DateTime _newInteractionDate = DateTime.Today;

    [ObservableProperty]
    private string _newInteractionSummary = string.Empty;

    [ObservableProperty]
    private DateTime? _newFollowUpDate;

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
    public void LogInteraction()
    {
        if (Contact.Id == 0) return;

        var interaction = new Interaction
        {
            ContactId = Contact.Id,
            Type = NewInteractionType,
            Date = NewInteractionDate,
            Summary = NewInteractionSummary,
            NextFollowUpDate = NewFollowUpDate
        };

        interaction.Id = _interactionRepo.Insert(interaction);
        Interactions.Insert(0, interaction);

        // Reset form
        NewInteractionSummary = string.Empty;
        NewFollowUpDate = null;
        NewInteractionDate = DateTime.Today;

        // Refresh last interaction date on the contact
        var updated = _contactRepo.GetById(Contact.Id);
        if (updated is not null)
            Contact = updated;
    }

    [RelayCommand]
    public void DeleteInteraction(Interaction interaction)
    {
        _interactionRepo.Delete(interaction.Id);
        Interactions.Remove(interaction);
    }
}

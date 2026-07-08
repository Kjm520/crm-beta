using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Haystack.Data;
using Haystack.Models;
using Haystack.ViewModels.Base;

namespace Haystack.ViewModels;

public partial class ActivityViewModel : ViewModelBase
{
    private readonly InteractionRepository _interactions = new();
    private readonly ContactRepository _contacts = new();

    // The activity feed: recent interactions across all contacts.
    [ObservableProperty]
    private ObservableCollection<Interaction> _activity = [];

    // Contacts available to log against, for the picker.
    [ObservableProperty]
    private ObservableCollection<Contact> _contactList = [];

    public InteractionType[] InteractionTypes { get; } = Enum.GetValues<InteractionType>();

    // New-interaction form fields.
    [ObservableProperty]
    private Contact? _selectedContact;

    [ObservableProperty]
    private InteractionType _newInteractionType = InteractionType.Call;

    [ObservableProperty]
    private DateTime _newInteractionDate = DateTime.Today;

    [ObservableProperty]
    private string _newInteractionSummary = string.Empty;

    [RelayCommand]
    public void Load()
    {
        ContactList = new ObservableCollection<Contact>(_contacts.GetAll());
        Activity = new ObservableCollection<Interaction>(_interactions.GetRecent(200));
    }

    private bool CanLog() => SelectedContact is not null;

    [RelayCommand(CanExecute = nameof(CanLog))]
    public void Log()
    {
        if (SelectedContact is null) return;

        var interaction = new Interaction
        {
            ContactId = SelectedContact.Id,
            Type = NewInteractionType,
            Date = NewInteractionDate,
            Summary = NewInteractionSummary
        };
        interaction.Id = _interactions.Insert(interaction);

        // Reflect the contact name so the new row shows correctly in the feed
        // without a full reload.
        interaction.ContactName = SelectedContact.Name;
        Activity.Insert(0, interaction);

        // Reset the form.
        NewInteractionSummary = string.Empty;
        NewInteractionDate = DateTime.Today;
        NewInteractionType = InteractionType.Call;
    }

    // Re-evaluate the Log button's enabled state when a contact is chosen.
    partial void OnSelectedContactChanged(Contact? value) => LogCommand.NotifyCanExecuteChanged();

    [RelayCommand]
    public void Delete(Interaction interaction)
    {
        _interactions.Delete(interaction.Id);
        Activity.Remove(interaction);
    }
}

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PulsePoint.Data;
using PulsePoint.Models;
using PulsePoint.ViewModels.Base;

namespace PulsePoint.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly ContactRepository _contacts = new();
    private readonly InteractionRepository _interactions = new();

    [ObservableProperty]
    private ObservableCollection<Contact> _staleContacts = [];

    [ObservableProperty]
    private ObservableCollection<Contact> _prospects = [];

    [ObservableProperty]
    private ObservableCollection<Interaction> _recentActivity = [];

    [ObservableProperty]
    private ObservableCollection<Interaction> _upcomingFollowUps = [];

    [ObservableProperty]
    private int _activeCustomerCount;

    [ObservableProperty]
    private int _prospectCount;

    [ObservableProperty]
    private int _staleThresholdDays = 60;

    [RelayCommand]
    public void Load()
    {
        var allContacts = _contacts.GetAll().ToList();

        ActiveCustomerCount = allContacts.Count(c => c.Type == ContactType.Customer && c.Status == ContactStatus.Active);
        ProspectCount = allContacts.Count(c => c.Type == ContactType.Prospect && c.Status == ContactStatus.Active);

        StaleContacts = new ObservableCollection<Contact>(
            _contacts.GetStale(StaleThresholdDays));

        Prospects = new ObservableCollection<Contact>(
            allContacts
                .Where(c => c.Type == ContactType.Prospect && c.Status == ContactStatus.Active)
                .OrderBy(c => c.DaysSinceLastInteraction)
                .Take(100));

        RecentActivity = new ObservableCollection<Interaction>(
            _interactions.GetRecent(15));

        UpcomingFollowUps = new ObservableCollection<Interaction>(
            _interactions.GetUpcomingFollowUps());
    }
}

using System.Windows;
using System.Windows.Input;
using Haystack.Models;
using Haystack.ViewModels;

namespace Haystack.Views.Contacts;

public partial class ContactDetailView : Window
{
    private readonly ContactDetailViewModel _vm = new();

    public event EventHandler? ContactSaved;

    public ContactDetailView(int? contactId)
    {
        InitializeComponent();

        _vm.ContactTypes = Enum.GetValues<ContactType>();
        _vm.ContactStatuses = Enum.GetValues<ContactStatus>();
        _vm.InteractionTypes = Enum.GetValues<InteractionType>();

        if (contactId.HasValue)
            _vm.LoadContact(contactId.Value);

        DataContext = _vm;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        => DragMove();

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        ContactSaved?.Invoke(this, EventArgs.Empty);
        Close();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
        => Close();
}

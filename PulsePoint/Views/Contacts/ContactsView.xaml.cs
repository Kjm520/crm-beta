using System.Windows;
using System.Windows.Controls;
using PulsePoint.ViewModels;
using PulsePoint.Views.Contacts;

namespace PulsePoint.Views.Contacts;

public partial class ContactsView : UserControl
{
    private readonly ContactsViewModel _vm = new();

    public ContactsView()
    {
        InitializeComponent();
        DataContext = _vm;
        _vm.LoadCommand.Execute(null);
    }

    private void NewContact_Click(object sender, RoutedEventArgs e)
    {
        var detail = new ContactDetailView(null);
        detail.ContactSaved += (_, _) => _vm.LoadCommand.Execute(null);
        detail.ShowDialog();
    }

    private void ContactList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_vm.SelectedContact is not null)
        {
            var detail = new ContactDetailView(_vm.SelectedContact.Id);
            detail.ContactSaved += (_, _) => _vm.LoadCommand.Execute(null);
            detail.ShowDialog();
            ContactList.SelectedItem = null;
        }
    }
}

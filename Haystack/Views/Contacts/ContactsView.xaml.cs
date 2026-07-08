using System.Windows;
using System.Windows.Controls;
using Haystack.ViewModels;
using Haystack.Views.Contacts;

namespace Haystack.Views.Contacts;

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
        detail.ShowDialog();
        // Reload after the dialog closes: interactions are written live, so the
        // list can be stale even if the contact itself wasn't re-saved.
        _vm.LoadCommand.Execute(null);
    }

    private void ContactList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_vm.SelectedContact is not null)
        {
            var detail = new ContactDetailView(_vm.SelectedContact.Id);
            detail.ShowDialog();
            ContactList.SelectedItem = null;
            _vm.LoadCommand.Execute(null);
        }
    }
}

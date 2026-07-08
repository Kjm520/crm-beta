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

    private void ContactRow_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: int contactId })
            return;

        var detail = new ContactDetailView(contactId);
        detail.ShowDialog();
        _vm.LoadCommand.Execute(null);
    }
}

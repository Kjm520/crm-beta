using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Haystack.Models;
using Haystack.ViewModels;
using Haystack.Views.Common;

namespace Haystack.Views.Contacts;

public partial class ContactsView : UserControl
{
    private readonly ContactsViewModel _vm = new();

    public ContactsView()
    {
        InitializeComponent();
        DataContext = _vm;
        _vm.LoadCommand.Execute(null);

        var spec = (TableColumns)Resources["Spec"];
        ListColumns.Apply(spec, ListColumns.ContactsKey);
        _ = new TableHeader(HeaderPanel, spec, RowsList, ListColumns.ContactsKey);
    }

    private void NewContact_Click(object sender, RoutedEventArgs e)
    {
        MainWindow.ShowModal(new ContactDetailView(null));
        // Reload after the dialog closes: interactions are written live, so the
        // list can be stale even if the contact itself wasn't re-saved.
        _vm.LoadCommand.Execute(null);
    }

    private void ContactRow_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ListBoxItem { Content: Contact contact })
            return;

        MainWindow.ShowModal(new ContactDetailView(contact.Id));
        _vm.LoadCommand.Execute(null);
    }
}

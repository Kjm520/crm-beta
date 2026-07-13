using System.Windows;
using System.Windows.Input;
using Haystack.Models;
using Haystack.ViewModels;
using Haystack.Views.Dialogs;

namespace Haystack.Views.Contacts;

public partial class ContactDetailView : Window
{
    private readonly ContactDetailViewModel _vm = new();

    public ContactDetailView(int? contactId, int? preselectCompanyId = null)
    {
        InitializeComponent();

        if (contactId.HasValue)
        {
            _vm.LoadContact(contactId.Value);
            DeleteButton.Visibility = Visibility.Visible;
        }
        else if (preselectCompanyId.HasValue)
        {
            _vm.PreselectCompany(preselectCompanyId.Value);
        }

        DataContext = _vm;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        => DragMove();

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var problem = _vm.Validate();
        if (problem is not null)
        {
            MessageBox.Show(this, problem, "Can't save yet",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _vm.SaveContact();
        Close();
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        var confirmed = ConfirmWindow.Ask(this,
            "Delete contact",
            _vm.Contact.Name,
            "Their logged interactions will be deleted too. This can't be undone.");

        if (!confirmed) return;

        _vm.DeleteContact();
        Close();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
        => Close();
}

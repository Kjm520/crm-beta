using System.Windows;
using System.Windows.Input;
using Haystack.Models;
using Haystack.ViewModels;

namespace Haystack.Views.Contacts;

public partial class ContactDetailView : Window
{
    private readonly ContactDetailViewModel _vm = new();

    public event EventHandler? ContactSaved;

    public ContactDetailView(int? contactId, int? preselectCompanyId = null)
    {
        InitializeComponent();

        if (contactId.HasValue)
            _vm.LoadContact(contactId.Value);
        else if (preselectCompanyId.HasValue)
            _vm.PreselectCompany(preselectCompanyId.Value);

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

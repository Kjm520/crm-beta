using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Haystack.ViewModels;
using Haystack.Views.Contacts;
using Haystack.Views.Dialogs;

namespace Haystack.Views.Companies;

public partial class CompanyDetailView : Window
{
    private readonly CompanyDetailViewModel _vm = new();

    public CompanyDetailView(int? companyId)
    {
        InitializeComponent();

        if (companyId.HasValue)
        {
            _vm.LoadCompany(companyId.Value);
            DeleteButton.Visibility = Visibility.Visible;
        }

        DataContext = _vm;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        => DragMove();

    private void AddContact_Click(object sender, RoutedEventArgs e)
    {
        // A contact needs a saved company to link to. If this company is new,
        // save it first so it has an Id.
        if (_vm.Company.Id == 0)
        {
            if (!_vm.SaveCompany())
            {
                MessageBox.Show(this,
                    "Enter a company name before adding contacts.",
                    "Company name required",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
        }

        var detail = new ContactDetailView(null, preselectCompanyId: _vm.Company.Id) { Owner = this };
        detail.ShowDialog();
        _vm.RefreshChildren(_vm.Company.Id);
    }

    private void ContactRow_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: int contactId })
            return;

        var detail = new ContactDetailView(contactId) { Owner = this };
        detail.ShowDialog();
        _vm.RefreshChildren(_vm.Company.Id);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var problem = _vm.Validate();
        if (problem is not null)
        {
            MessageBox.Show(this, problem, "Can't save yet",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _vm.SaveCompany();
        Close();
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        var contactCount = _vm.Contacts.Count;
        var cascade = contactCount == 0
            ? "This company has no contacts."
            : $"This will also delete its {contactCount} contact(s) and all their logged interactions.";

        var confirmed = ConfirmWindow.Ask(this,
            "Delete company",
            _vm.Company.Name,
            $"{cascade} This can't be undone.");

        if (!confirmed) return;

        _vm.DeleteCompany();
        Close();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
        => Close();
}

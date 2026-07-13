using System.Windows;
using System.Windows.Input;
using Haystack.Data;
using Haystack.ViewModels;
using Haystack.Views.Dialogs;

namespace Haystack.Views.Leads;

public partial class LeadDetailView : Window
{
    private readonly LeadDetailViewModel _vm = new();
    private readonly LeadRepository _repo = new();

    public LeadDetailView(int? leadId)
    {
        InitializeComponent();

        if (leadId.HasValue)
        {
            _vm.LoadLead(leadId.Value);
            DeleteButton.Visibility = Visibility.Visible;
            // Only offer conversion for a saved, not-yet-converted lead.
            ConvertButton.Visibility = _vm.Lead.IsConverted
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        DataContext = _vm;

        // Fast entry: land the caret in the first field (a lead is a company).
        Loaded += (_, _) => CompanyField.Focus();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        => DragMove();

    // Enter saves (unless focus is in the multi-line notes box, where Enter is a newline).
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Enter && Keyboard.FocusedElement is not System.Windows.Controls.TextBox { AcceptsReturn: true })
        {
            Save_Click(this, new RoutedEventArgs());
            e.Handled = true;
        }
        base.OnKeyDown(e);
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

        _vm.SaveLead();
        Close();
    }

    private void Convert_Click(object sender, RoutedEventArgs e)
    {
        var problem = _vm.ValidateConvert();
        if (problem is not null)
        {
            MessageBox.Show(this, problem, "Can't convert",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirm = MessageBox.Show(this,
            $"Create a company from this lead?\n\n{_vm.Lead.Company}\n\n" +
            "The lead is kept and marked as won, linked to the new company.",
            "Convert lead",
            MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        _vm.ConvertToCompany();

        MessageBox.Show(this,
            $"\"{_vm.Lead.Company}\" is now a company.\n\nAdd contacts to it from the Companies tab.",
            "Lead converted",
            MessageBoxButton.OK, MessageBoxImage.Information);

        Close();
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        var confirmed = ConfirmWindow.Ask(this,
            "Delete lead",
            _vm.Lead.DisplayName,
            "This can't be undone.");

        if (!confirmed) return;

        _repo.Delete(_vm.Lead.Id);
        Close();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
        => Close();
}

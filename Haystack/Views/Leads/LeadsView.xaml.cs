using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Haystack.Models;
using Haystack.ViewModels;
using Haystack.Views.Common;

namespace Haystack.Views.Leads;

public partial class LeadsView : UserControl
{
    private readonly LeadsViewModel _vm = new();

    public LeadsView()
    {
        InitializeComponent();
        DataContext = _vm;
        _vm.LoadCommand.Execute(null);

        var spec = (TableColumns)Resources["Spec"];
        ListColumns.Apply(spec, ListColumns.LeadsKey);
        _ = new TableHeader(HeaderPanel, spec, RowsList, ListColumns.LeadsKey);
    }

    private void NewLead_Click(object sender, RoutedEventArgs e)
    {
        MainWindow.ShowModal(new LeadDetailView(null));
        _vm.LoadCommand.Execute(null);
    }

    private void LeadRow_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ListBoxItem { Content: Lead lead })
            return;

        MainWindow.ShowModal(new LeadDetailView(lead.Id));
        _vm.LoadCommand.Execute(null);
    }
}

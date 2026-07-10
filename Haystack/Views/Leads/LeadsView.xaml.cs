using System.Windows;
using System.Windows.Controls;
using Haystack.ViewModels;

namespace Haystack.Views.Leads;

public partial class LeadsView : UserControl
{
    private readonly LeadsViewModel _vm = new();

    public LeadsView()
    {
        InitializeComponent();
        DataContext = _vm;
        _vm.LoadCommand.Execute(null);
    }

    private void NewLead_Click(object sender, RoutedEventArgs e)
    {
        MainWindow.ShowModal(new LeadDetailView(null));
        _vm.LoadCommand.Execute(null);
    }

    private void LeadRow_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: int leadId })
            return;

        MainWindow.ShowModal(new LeadDetailView(leadId));
        _vm.LoadCommand.Execute(null);
    }
}

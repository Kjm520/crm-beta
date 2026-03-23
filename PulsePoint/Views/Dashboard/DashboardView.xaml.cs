using System.Windows.Controls;
using PulsePoint.ViewModels;

namespace PulsePoint.Views.Dashboard;

public partial class DashboardView : UserControl
{
    private readonly DashboardViewModel _vm = new();

    public DashboardView()
    {
        InitializeComponent();
        DataContext = _vm;
        _vm.LoadCommand.Execute(null);
    }
}

using System.Windows.Controls;
using Haystack.ViewModels;

namespace Haystack.Views.Dashboard;

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

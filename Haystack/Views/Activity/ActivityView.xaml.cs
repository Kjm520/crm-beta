using System.Windows.Controls;
using Haystack.ViewModels;

namespace Haystack.Views.Activity;

public partial class ActivityView : UserControl
{
    private readonly ActivityViewModel _vm = new();

    public ActivityView()
    {
        InitializeComponent();
        DataContext = _vm;
        _vm.LoadCommand.Execute(null);
    }
}

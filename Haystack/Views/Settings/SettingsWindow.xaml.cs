using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Haystack.Models;
using Haystack.Navigation;
using Haystack.ViewModels;

namespace Haystack.Views.Settings;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _vm = new();

    // Nav drag-reorder state.
    private NavItem? _navDragItem;
    private Point _navDragStart;

    public SettingsWindow()
    {
        InitializeComponent();
        DataContext = _vm;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        => DragMove();

    // --- Nav tab drag-to-reorder -----------------------------------------

    private void NavHandle_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: NavItem item })
        {
            _navDragItem = item;
            _navDragStart = e.GetPosition(null);
        }
    }

    private void NavHandle_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_navDragItem is null || e.LeftButton != MouseButtonState.Pressed) return;

        var pos = e.GetPosition(null);
        if (Math.Abs(pos.X - _navDragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(pos.Y - _navDragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;

        var dragged = _navDragItem;
        _navDragItem = null; // clear before the blocking drag loop
        DragDrop.DoDragDrop((DependencyObject)sender, dragged, DragDropEffects.Move);
    }

    private void NavRow_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(typeof(NavItem))
            ? DragDropEffects.Move
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void NavRow_Drop(object sender, DragEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: NavItem target } &&
            e.Data.GetData(typeof(NavItem)) is NavItem dragged)
        {
            _vm.MoveNav(dragged, target);
        }
    }

    private void AddStatus_Click(object sender, RoutedEventArgs e)
    {
        var problem = _vm.AddStatus();
        if (problem is not null)
            MessageBox.Show(this, problem, "Can't add status",
                MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void RenameStatus_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: LeadStatus status }) return;

        var prompt = new TextPromptWindow("Rename status", "New name:", status.Name) { Owner = this };
        if (prompt.ShowDialog() != true) return;

        var problem = _vm.RenameStatus(status, prompt.Value);
        if (problem is not null)
            MessageBox.Show(this, problem, "Can't rename status",
                MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void DeleteStatus_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: LeadStatus status }) return;

        var message = status.LeadCount > 0
            ? $"Delete the status \"{status.Name}\"?\n\n{status.LeadCount} lead(s) use it. They will be kept, but become unassigned."
            : $"Delete the status \"{status.Name}\"?";

        var confirm = MessageBox.Show(this, message, "Delete status",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (confirm == MessageBoxResult.Yes)
            _vm.DeleteStatusCommand.Execute(status);
    }

    private void Close_Click(object sender, RoutedEventArgs e)
        => Close();
}

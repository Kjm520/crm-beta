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

    public SettingsWindow()
    {
        InitializeComponent();
        DataContext = _vm;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        => DragMove();

    private void MoveNavUp_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: NavItem item }) _vm.MoveNavUp(item);
    }

    private void MoveNavDown_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: NavItem item }) _vm.MoveNavDown(item);
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

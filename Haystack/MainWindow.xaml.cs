using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using Haystack.Data;
using Haystack.Views.Dashboard;
using Haystack.Views.Contacts;
using Haystack.Views.Workflow;
using Haystack.Views.Activity;

namespace Haystack;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        MainContent.Content = new DashboardView();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
            ToggleMaximize();
        else
            DragMove();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        => ToggleMaximize();

    private void CloseButton_Click(object sender, RoutedEventArgs e)
        => Close();

    private void ToggleMaximize()
        => WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;

    private void Nav_Dashboard(object sender, RoutedEventArgs e)
    {
        if (MainContent is null) return;
        MainContent.Content = new DashboardView();
    }

    private void Nav_Contacts(object sender, RoutedEventArgs e)
    {
        if (MainContent is null) return;
        MainContent.Content = new ContactsView();
    }

    private void Nav_Workflow(object sender, RoutedEventArgs e)
    {
        if (MainContent is null) return;
        MainContent.Content = new WorkflowView();
    }

    private void Nav_Activity(object sender, RoutedEventArgs e)
    {
        if (MainContent is null) return;
        MainContent.Content = new ActivityView();
    }

    private void BackupDatabase_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Backup Haystack Database",
            Filter = "Haystack backup (*.db)|*.db|All files (*.*)|*.*",
            DefaultExt = ".db",
            FileName = $"haystack-backup-{DateTime.Now:yyyy-MM-dd}.db"
        };

        if (dialog.ShowDialog(this) != true)
            return;

        try
        {
            Database.Backup(dialog.FileName);
            MessageBox.Show(this,
                $"Backup saved to:\n{dialog.FileName}",
                "Backup complete",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                $"Backup failed:\n{ex.Message}",
                "Backup error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

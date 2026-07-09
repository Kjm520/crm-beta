using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using Haystack.Data;
using Haystack.Views.Dashboard;
using Haystack.Views.Contacts;
using Haystack.Views.Workflow;
using Haystack.Views.Activity;
using Haystack.Views.Companies;
using Haystack.Views.Settings;

namespace Haystack;

public partial class MainWindow : Window
{
    /// <summary>
    /// Shows a dialog modally with the background dimmed by the main-window scrim
    /// (which quickly fades in/out), signalling that the rest of the app is blocked
    /// until the dialog closes. Safe to call from any view.
    /// </summary>
    public static bool? ShowModal(Window dialog)
    {
        var main = Application.Current.MainWindow as MainWindow;
        dialog.Owner = main;
        main?.FadeScrim(true);
        try
        {
            return dialog.ShowDialog();
        }
        finally
        {
            main?.FadeScrim(false);
        }
    }

    private void FadeScrim(bool show)
    {
        if (show)
            ModalScrim.Visibility = Visibility.Visible;

        var fade = new System.Windows.Media.Animation.DoubleAnimation
        {
            To = show ? 1.0 : 0.0,
            Duration = TimeSpan.FromMilliseconds(60),
            EasingFunction = new System.Windows.Media.Animation.QuadraticEase()
        };
        if (!show)
            fade.Completed += (_, _) => ModalScrim.Visibility = Visibility.Collapsed;

        ModalScrim.BeginAnimation(OpacityProperty, fade);

        // Let the fade-in render a frame before ShowDialog() blocks the thread,
        // so the scrim eases in rather than snapping to dark.
        if (show)
            Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);
    }

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

    private void Nav_Companies(object sender, RoutedEventArgs e)
    {
        if (MainContent is null) return;
        MainContent.Content = new CompaniesView();
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

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        ShowModal(new SettingsWindow());
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

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PulsePoint.Views.Dashboard;
using PulsePoint.Views.Contacts;

namespace PulsePoint;

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
}

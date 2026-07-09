using System.Windows;
using System.Windows.Input;

namespace Haystack.Views.Settings;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        => DragMove();

    private void Close_Click(object sender, RoutedEventArgs e)
        => Close();
}

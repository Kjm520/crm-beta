using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using Haystack.Data;
using Haystack.Navigation;
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

        // Fade the dialog window in together with the scrim so they read as one
        // motion (the scrim lives in the main window; the dialog is its own Window).
        dialog.Opacity = 0;
        dialog.Loaded += (_, _) =>
        {
            var fadeIn = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0, To = 1,
                Duration = TimeSpan.FromMilliseconds(0),
                EasingFunction = new System.Windows.Media.Animation.QuadraticEase()
            };
            dialog.BeginAnimation(OpacityProperty, fadeIn);
        };

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
            // Explicit From so the scrim always animates the FULL range. Without it,
            // WPF starts from the property's current (held) value, which caused the
            // scrim to jump most of the way to dark, then crawl the rest.
            From = show ? 0.0 : 0.5,
            To   = show ? 0.5 : 0.0,
            // Separate in/out durations (ms).
            Duration = TimeSpan.FromMilliseconds(show ? 600 : 100),
            EasingFunction = new System.Windows.Media.Animation.CircleEase()
        };
        if (!show)
            fade.Completed += (_, _) => ModalScrim.Visibility = Visibility.Collapsed;

        ModalScrim.BeginAnimation(OpacityProperty, fade);
    }

    private readonly AppSettingsRepository _appSettings = new();

    public MainWindow()
    {
        InitializeComponent();
        BuildNav();
    }

    /// <summary>
    /// (Re)builds the left-panel nav buttons from NavRegistry in the user's saved
    /// order. Preserves the currently selected tab across a rebuild.
    /// </summary>
    private void BuildNav()
    {
        // Remember which tab is selected so a reorder doesn't jump us elsewhere.
        string? currentKey = null;
        foreach (var child in NavPanel.Children)
            if (child is RadioButton { IsChecked: true, Tag: NavItem sel }) { currentKey = sel.Key; break; }

        // Clear existing nav buttons but keep the MENU header (a TextBlock).
        for (var i = NavPanel.Children.Count - 1; i >= 0; i--)
            if (NavPanel.Children[i] is RadioButton) NavPanel.Children.RemoveAt(i);

        var items = NavRegistry.InSavedOrder(_appSettings.Get(NavRegistry.OrderSettingKey));
        var navStyle = (Style)FindResource("NavButton");

        RadioButton? first = null, toSelect = null;
        foreach (var item in items)
        {
            var rb = new RadioButton { Style = navStyle, Content = item.Label, Tag = item };
            rb.Checked += NavItem_Checked;
            NavPanel.Children.Add(rb);

            first ??= rb;
            if (item.Key == currentKey) toSelect = rb;
        }

        // Selecting fires Checked, which loads the view.
        if (toSelect is not null) toSelect.IsChecked = true;
        else if (first is not null) first.IsChecked = true;
    }

    private void NavItem_Checked(object sender, RoutedEventArgs e)
    {
        if (MainContent is not null && sender is RadioButton { Tag: NavItem item })
            MainContent.Content = item.CreateView();
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

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        // Rebuild the nav only if the tab order actually changed in Settings, so
        // simply opening/closing Settings doesn't reset the current tab.
        var before = _appSettings.Get(NavRegistry.OrderSettingKey);
        ShowModal(new SettingsWindow());
        var after = _appSettings.Get(NavRegistry.OrderSettingKey);
        if (before != after) BuildNav();
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

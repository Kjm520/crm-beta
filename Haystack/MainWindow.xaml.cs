using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using Haystack.Alerts;
using Haystack.Data;
using Haystack.Models;
using Haystack.Navigation;
using Haystack.Views.Companies;
using Haystack.Views.Contacts;
using Haystack.Views.Dialogs;
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
            // Modals are where activity gets logged and cadences get edited, so
            // re-derive the alert bell whenever one closes.
            main?.RefreshAlerts();
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

        // Right-align the alerts popup under the bell (stock Bottom placement
        // left-aligns, which would hang the panel off the window's right edge).
        AlertsPopup.Placement = PlacementMode.Custom;
        AlertsPopup.CustomPopupPlacementCallback = (size, target, _) =>
            [new CustomPopupPlacement(
                new Point(target.Width - size.Width, target.Height + 6),
                PopupPrimaryAxis.Horizontal)];

        RefreshAlerts();

        // Slow rollover tick: day boundaries change how overdue things are even
        // with no user action, so an app left open overnight stays truthful.
        var alertTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(30) };
        alertTimer.Tick += (_, _) => RefreshAlerts();
        alertTimer.Start();
    }

    // --- Alert bell ---------------------------------------------------------

    private void AlertsButton_Checked(object sender, RoutedEventArgs e) => RefreshAlerts();

    /// <summary>
    /// Runs the monitors (which materialize/resolve Workflow tasks), then
    /// re-derives the bell from the result. Called at startup, whenever any
    /// modal closes, when the bell is opened, and on the rollover timer.
    /// </summary>
    private void RefreshAlerts()
    {
        MonitorEngine.Run();
        var alerts = AlertCenter.GatherAll();

        AlertBadge.Visibility = alerts.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        AlertCount.Text = alerts.Count > 9 ? "9+" : alerts.Count.ToString();

        BuildAlertsPanel(alerts);
    }

    private void BuildAlertsPanel(List<Alert> alerts)
    {
        AlertsList.Children.Clear();

        if (alerts.Count == 0)
        {
            AlertsList.Children.Add(new TextBlock
            {
                Text = "Nothing needs attention.",
                Style = (Style)FindResource("TextBodySecondary"),
                Margin = new Thickness(10, 8, 10, 8),
            });
            return;
        }

        var itemStyle = (Style)FindResource("MenuDropItem");
        var titleStyle = (Style)FindResource("TextBody");
        var captionStyle = (Style)FindResource("TextCaption");

        foreach (var group in alerts.GroupBy(a => a.Group))
        {
            AlertsList.Children.Add(new TextBlock
            {
                Text = group.Key,
                Style = captionStyle,
                Margin = new Thickness(10, 8, 10, 4),
            });

            foreach (var alert in group)
            {
                var content = new StackPanel();
                content.Children.Add(new TextBlock { Text = alert.Title, Style = titleStyle });
                content.Children.Add(new TextBlock
                {
                    Text = alert.Detail,
                    Style = captionStyle,
                    Margin = new Thickness(0, 1, 0, 0),
                });

                var item = new Button { Style = itemStyle, Content = content };
                var opened = alert;
                item.Click += (_, _) => OpenAlert(opened);
                AlertsList.Children.Add(item);
            }
        }
    }

    private void OpenAlert(Alert alert)
    {
        AlertsButton.IsChecked = false;

        ShowModal(alert.Target switch
        {
            RecordKind.Company => new CompanyDetailView(alert.TargetId),
            RecordKind.Contact => new ContactDetailView(alert.TargetId),
            _ => throw new ArgumentOutOfRangeException(nameof(alert)),
        });
        // ShowModal already re-derives the alerts when the modal closes.
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

    // File-menu items: close the dropdown, then run the action.
    private void FileMenuSettings_Click(object sender, RoutedEventArgs e)
    {
        FileMenuButton.IsChecked = false;
        Settings_Click(sender, e);
    }

    private void FileMenuBackup_Click(object sender, RoutedEventArgs e)
    {
        FileMenuButton.IsChecked = false;
        BackupDatabase_Click(sender, e);
    }

    private void FileMenuExit_Click(object sender, RoutedEventArgs e)
    {
        FileMenuButton.IsChecked = false;
        Close();
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        // Rebuild the nav only if the tab order actually changed in Settings, so
        // simply opening/closing Settings doesn't reset the current tab. (Column
        // choices don't need this: they're managed live on the tables themselves,
        // via the header's right-click menu.)
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
            ConfirmWindow.Alert(this, "Backup complete",
                dialog.FileName,
                subject: "Backup saved to:");
        }
        catch (Exception ex)
        {
            ConfirmWindow.Alert(this, "Backup error",
                ex.Message,
                subject: "Backup failed.");
        }
    }
}

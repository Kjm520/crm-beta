using System.Windows;
using System.Windows.Input;

namespace Haystack.Views.Dialogs;

/// <summary>
/// Themed replacement for MessageBox's Yes/No confirmation. The native box can't be
/// styled at all, so it broke the app's look (and ignored dark mode entirely).
///
/// Call it through <see cref="Ask"/>: it returns true only when the user actively
/// confirms. Destructive calls get the red confirm button and leave focus on Cancel,
/// so a stray Enter or Space can't delete anything.
/// </summary>
public partial class ConfirmWindow : Window
{
    private ConfirmWindow(string title, string subject, string message,
                          string confirmLabel, bool destructive)
    {
        InitializeComponent();

        TitleText.Text = title;
        SubjectText.Text = subject;
        MessageText.Text = message;
        ConfirmButton.Content = confirmLabel;

        // No subject (e.g. a plain question) — don't leave an empty line behind.
        if (string.IsNullOrWhiteSpace(subject))
            SubjectText.Visibility = Visibility.Collapsed;

        ConfirmButton.Style = (Style)FindResource(destructive ? "ButtonDanger" : "ButtonPrimary");

        if (destructive)
            Loaded += (_, _) => CancelButton.Focus();
        else
            ConfirmButton.IsDefault = true;
    }

    /// <summary>
    /// Asks the user to confirm an action. Returns true only if they confirmed.
    /// </summary>
    /// <param name="subject">What's being acted on (a name), shown emphasized.</param>
    /// <param name="message">What the action will do — cascades, irreversibility.</param>
    public static bool Ask(Window owner, string title, string subject, string message,
                           string confirmLabel = "Delete", bool destructive = true)
    {
        var dialog = new ConfirmWindow(title, subject, message, confirmLabel, destructive)
        {
            Owner = owner
        };
        return dialog.ShowDialog() == true;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        => DragMove();

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

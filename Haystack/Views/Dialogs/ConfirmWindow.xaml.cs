using System.Windows;
using System.Windows.Input;

namespace Haystack.Views.Dialogs;

/// <summary>
/// Themed replacement for MessageBox. The native box can't be styled at all, so it
/// broke the app's look (and ignored dark mode entirely). This covers both shapes
/// the app needs:
///
/// <see cref="Ask"/> — a two-button confirmation, returning true only when the user
/// actively confirms. Destructive calls get the red confirm button and leave focus on
/// Cancel, so a stray Enter or Space can't delete anything.
///
/// <see cref="Alert"/> — a one-button notice (validation, success, failure). Same
/// window with the Cancel button dropped, so the two never drift apart visually.
/// </summary>
public partial class ConfirmWindow : Window
{
    private ConfirmWindow(string title, string subject, string message,
                          string confirmLabel, bool destructive, bool cancellable)
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

        if (!cancellable)
        {
            // A notice has nothing to cancel: one button, which Esc also closes.
            CancelButton.Visibility = Visibility.Collapsed;
            ConfirmButton.IsCancel = true;
        }

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
        var dialog = new ConfirmWindow(title, subject, message, confirmLabel, destructive,
                                       cancellable: true)
        {
            Owner = owner
        };
        return dialog.ShowDialog() == true;
    }

    /// <summary>
    /// Tells the user something and waits for acknowledgement — the themed stand-in
    /// for a one-button MessageBox (validation problems, results, errors).
    /// </summary>
    public static void Alert(Window owner, string title, string message, string subject = "")
    {
        new ConfirmWindow(title, subject, message, "OK", destructive: false, cancellable: false)
        {
            Owner = owner
        }.ShowDialog();
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

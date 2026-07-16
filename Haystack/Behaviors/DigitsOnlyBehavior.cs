using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Haystack.Behaviors;

/// <summary>
/// Restricts a TextBox to bare digits — typed, pasted, or spaced in. Used for
/// small numeric fields like the "contact every N days" cadence, where letting
/// stray characters through would just make the binding silently refuse to
/// update the (int?) source.
///
/// Usage:  &lt;TextBox behaviors:DigitsOnlyBehavior.Enabled="True" ... /&gt;
/// </summary>
public static class DigitsOnlyBehavior
{
    public static readonly DependencyProperty EnabledProperty =
        DependencyProperty.RegisterAttached(
            "Enabled", typeof(bool), typeof(DigitsOnlyBehavior),
            new PropertyMetadata(false, OnEnabledChanged));

    public static void SetEnabled(DependencyObject o, bool value) => o.SetValue(EnabledProperty, value);
    public static bool GetEnabled(DependencyObject o) => (bool)o.GetValue(EnabledProperty);

    private static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox box) return;

        if ((bool)e.NewValue)
        {
            box.PreviewTextInput += OnPreviewTextInput;
            box.PreviewKeyDown += OnPreviewKeyDown;
            DataObject.AddPastingHandler(box, OnPaste);
        }
        else
        {
            box.PreviewTextInput -= OnPreviewTextInput;
            box.PreviewKeyDown -= OnPreviewKeyDown;
            DataObject.RemovePastingHandler(box, OnPaste);
        }
    }

    private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        => e.Handled = !e.Text.All(char.IsDigit);

    // Space doesn't raise PreviewTextInput, so block it here.
    private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
        => e.Handled = e.Key == Key.Space;

    private static void OnPaste(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetData(typeof(string)) is not string text || !text.All(char.IsDigit))
            e.CancelCommand();
    }
}

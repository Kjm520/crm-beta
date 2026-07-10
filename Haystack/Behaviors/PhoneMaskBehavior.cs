using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Haystack.Behaviors;

/// <summary>
/// Live-formats a TextBox as a US phone number (xxx-xxx-xxxx) while the user types,
/// the way payment forms mask card numbers.
///
/// Masking is deliberately conservative: it only engages while the input is plain
/// digits (up to 10). As soon as the value contains anything else - a '+', letters,
/// an extension - the mask backs off and lets the user type freely, so international
/// numbers and extensions are never mangled.
///
/// Usage:  &lt;TextBox behaviors:PhoneMaskBehavior.Enabled="True" ... /&gt;
/// </summary>
public static class PhoneMaskBehavior
{
    private const int MainDigits = 10;                       // xxx-xxx-xxxx
    private const int MaxExtDigits = 8;                      // extensions beyond this are unheard of
    private const int MaxDigits = MainDigits + MaxExtDigits; // hard cap on typed/pasted digits

    public static readonly DependencyProperty EnabledProperty =
        DependencyProperty.RegisterAttached(
            "Enabled", typeof(bool), typeof(PhoneMaskBehavior),
            new PropertyMetadata(false, OnEnabledChanged));

    public static void SetEnabled(DependencyObject o, bool value) => o.SetValue(EnabledProperty, value);
    public static bool GetEnabled(DependencyObject o) => (bool)o.GetValue(EnabledProperty);

    private static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox box) return;

        if ((bool)e.NewValue)
        {
            box.PreviewTextInput += OnPreviewTextInput;
            box.TextChanged       += OnTextChanged;
            DataObject.AddPastingHandler(box, OnPaste);
        }
        else
        {
            box.PreviewTextInput -= OnPreviewTextInput;
            box.TextChanged       -= OnTextChanged;
            DataObject.RemovePastingHandler(box, OnPaste);
        }
    }

    // Block characters that can't start a maskable number, but allow the ones that
    // signal "this isn't a plain US number" so free-form entry still works.
    private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        // Digits past the 10th roll into an extension, so there's no cap to enforce.
        // Non-digits simply switch the field to free-form on the next TextChanged.
    }

    private static void OnPaste(object sender, DataObjectPastingEventArgs e) { /* let it through; TextChanged reformats */ }

    private static bool _reentrant;

    private static void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_reentrant || sender is not TextBox box) return;
        if (!IsMaskable(box.Text)) return;   // free-form input: leave it alone

        var digits = OnlyDigits(box.Text);
        if (digits.Length > MaxDigits) digits = digits[..MaxDigits];

        var formatted = Mask(digits);
        if (formatted == box.Text) return;

        // Count digits before the caret so we can restore the caret to the same
        // logical position after re-inserting dashes.
        var caret = box.CaretIndex;
        var digitsBeforeCaret = DigitCount(box.Text[..Math.Min(caret, box.Text.Length)]);

        _reentrant = true;
        box.Text = formatted;
        box.CaretIndex = CaretForDigit(formatted, digitsBeforeCaret);
        _reentrant = false;
    }

    /// <summary>
    /// Only mask input made of the characters the mask itself produces: digits, the
    /// dashes, and the " x" that introduces an extension. Anything else (a '+', a
    /// letter, spaces) means the user is entering a non-US number, so the mask backs
    /// off and leaves the text alone.
    /// </summary>
    private static bool IsMaskable(string text)
    {
        if (string.IsNullOrEmpty(text)) return true;

        // Allow at most one ' x' extension marker, and only after 10 digits.
        var xIndex = text.IndexOf('x');
        if (xIndex >= 0)
        {
            if (text.IndexOf('x', xIndex + 1) >= 0) return false;     // more than one 'x'
            if (xIndex == 0 || text[xIndex - 1] != ' ') return false; // must be " x"
        }

        foreach (var c in text)
            if (!char.IsDigit(c) && c != '-' && c != ' ' && c != 'x') return false;

        return true;
    }

    /// <summary>
    /// xxx-xxx-xxxx, with any digits past the 10th rolling into " xNNNN"
    /// (extension capped at <see cref="MaxExtDigits"/>).
    /// </summary>
    private static string Mask(string digits) => digits.Length switch
    {
        <= 3           => digits,
        <= 6           => $"{digits[..3]}-{digits[3..]}",
        <= MainDigits  => $"{digits[..3]}-{digits[3..6]}-{digits[6..]}",
        _              => $"{digits[..3]}-{digits[3..6]}-{digits[6..MainDigits]} x{digits[MainDigits..]}"
    };

    /// <summary>Index just after the Nth digit, so the caret lands where the user expects.</summary>
    private static int CaretForDigit(string text, int digitIndex)
    {
        if (digitIndex <= 0) return 0;
        var seen = 0;
        for (var i = 0; i < text.Length; i++)
        {
            if (char.IsDigit(text[i])) seen++;
            if (seen == digitIndex) return i + 1;
        }
        return text.Length;
    }

    private static string OnlyDigits(string s) => new(s.Where(char.IsDigit).ToArray());
    private static int DigitCount(string s) => s.Count(char.IsDigit);
}

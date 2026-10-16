using System.Text;

namespace Haystack.Models;

/// <summary>
/// Phone numbers are STORED as raw digits and FORMATTED only for display. That way
/// "(555) 555-0123", "5555550123" and "555.555.0123" all normalize to one value,
/// so searching and comparison work, and the display format can change later.
///
/// Anything that isn't a clean 10-digit US number (extensions, international, partial
/// entries) is preserved as typed rather than mangled into a format it doesn't fit.
/// </summary>
public static class PhoneNumber
{
    /// <summary>Extensions longer than this are unheard of; excess digits are dropped.</summary>
    public const int MaxExtensionDigits = 8;

    /// <summary>
    /// Strips formatting from a US 10-digit number so it stores as digits only.
    /// Non-standard input (international, extensions, partial) is returned trimmed
    /// but otherwise untouched.
    /// </summary>
    public static string? Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;

        // Split off an extension ("... x123") before looking at the main number.
        var (main, ext) = SplitExtension(input.Trim());

        var digits = Digits(main);

        // 11 digits starting with the US country code -> drop the leading 1.
        if (digits.Length == 11 && digits[0] == '1') digits = digits[1..];

        // Exactly 10 digits -> a US number; store bare digits (+ ext, if any).
        if (digits.Length == 10)
            return ext is null ? digits : $"{digits}x{ext}";

        // Anything else (international, partial): keep what the user typed.
        return input.Trim();
    }

    /// <summary>
    /// Formats a stored value as xxx-xxx-xxxx (plus " xNNN" when it has an extension)
    /// when the main part is a clean 10-digit number. Otherwise returns it unchanged.
    /// </summary>
    public static string? Format(string? stored)
    {
        if (string.IsNullOrWhiteSpace(stored)) return stored;

        var (main, ext) = SplitExtension(stored.Trim());

        var digits = Digits(main);
        if (digits.Length != 10) return stored;

        var formatted = $"{digits[..3]}-{digits[3..6]}-{digits[6..]}";
        return ext is null ? formatted : $"{formatted} x{ext}";
    }

    /// <summary>
    /// Splits "5555550123x123" or "555-555-0123 x123" into main + extension digits.
    /// The extension is truncated to <see cref="MaxExtensionDigits"/>.
    /// </summary>
    private static (string Main, string? Ext) SplitExtension(string s)
    {
        var i = s.IndexOf('x', StringComparison.OrdinalIgnoreCase);
        if (i < 0) return (s, null);

        var ext = Digits(s[(i + 1)..]);
        if (ext.Length > MaxExtensionDigits) ext = ext[..MaxExtensionDigits];

        return (s[..i], ext.Length == 0 ? null : ext);
    }

    private static string Digits(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (var c in s)
            if (char.IsDigit(c)) sb.Append(c);
        return sb.ToString();
    }
}

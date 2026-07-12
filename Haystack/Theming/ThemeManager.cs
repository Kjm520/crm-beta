using System.Windows;
using Haystack.Data;

namespace Haystack.Theming;

/// <summary>
/// Swaps the active colour palette (light/dark) at runtime by replacing the
/// palette ResourceDictionary in Application.Resources. The theme brushes pull
/// their colours from that palette via DynamicResource, so replacing it recolours
/// every open window live — no restart, no per-window work.
/// </summary>
public static class ThemeManager
{
    public const string SettingKey = "theme";
    public const string Light = "light";
    public const string Dark = "dark";

    private static readonly AppSettingsRepository _settings = new();

    /// <summary>The theme currently applied.</summary>
    public static string Current { get; private set; } = Light;

    public static bool IsDark => Current == Dark;

    /// <summary>The theme saved in app settings (defaults to light).</summary>
    public static string Saved => _settings.Get(SettingKey) == Dark ? Dark : Light;

    /// <summary>
    /// Applies a theme. Replaces the palette dictionary (first merged dictionary
    /// whose source is a "Palette.*") and, unless told otherwise, persists it.
    /// </summary>
    public static void Apply(string theme, bool persist = true)
    {
        theme = theme == Dark ? Dark : Light;
        Current = theme;

        var file = theme == Dark ? "Palette.Dark.xaml" : "Palette.Light.xaml";
        var newPalette = new ResourceDictionary
        {
            Source = new Uri(
                $"pack://application:,,,/Haystack;component/Resources/Themes/{file}",
                UriKind.Absolute)
        };

        var dicts = Application.Current.Resources.MergedDictionaries;
        for (var i = dicts.Count - 1; i >= 0; i--)
        {
            var src = dicts[i].Source?.OriginalString ?? string.Empty;
            if (src.Contains("Palette.", StringComparison.OrdinalIgnoreCase))
                dicts.RemoveAt(i);
        }
        dicts.Insert(0, newPalette);

        if (persist) _settings.Set(SettingKey, theme);
    }

    /// <summary>Applies the saved theme at startup (does not re-persist).</summary>
    public static void ApplySaved() => Apply(Saved, persist: false);
}

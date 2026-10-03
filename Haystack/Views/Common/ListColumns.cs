using System.Windows;
using Haystack.Data;

namespace Haystack.Views.Common;

/// <summary>
/// Load/save/apply for the list tables' user column choices: which columns are
/// toggled away from their designed visibility and any widths they've been
/// resized to · both managed live on the tables themselves via the header's
/// right-click menu (see TableHeader), and applied back to a view's column spec
/// at construction, before first layout, so a table only ever renders in its
/// chosen shape.
///
/// Stored per table in app_settings as CSVs · headers whose visibility differs
/// from the XAML default (so a default-hidden column the user shows persists,
/// exactly like a default-shown column they hide), and "header:pixels" width
/// overrides · keyed by header text rather than index, so a saved choice
/// survives columns being added or reordered in code later.
/// </summary>
public static class ListColumns
{
    public const string ContactsKey  = "columns.contacts";
    public const string CompaniesKey = "columns.companies";
    public const string LeadsKey     = "columns.leads";

    private static HashSet<string> Toggled(string settingsKey) =>
        (new AppSettingsRepository().Get(settingsKey) ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet();

    /// <summary>
    /// Snapshots every column whose visibility differs from its XAML-designed
    /// default. Called by TableHeader after each show/hide toggle.
    /// </summary>
    public static void SaveVisibility(string settingsKey, TableColumns columns) =>
        new AppSettingsRepository().Set(settingsKey, string.Join(",",
            columns.Where(c => c.Visible != (c.DefaultVisible ?? true))
                   .Select(c => c.Header)));

    /// <summary>
    /// Applies the saved choices (hidden columns and resized widths) to a list
    /// view's column spec. Called from the view's constructor, before first
    /// layout, so a table never flashes its default shape.
    /// </summary>
    public static void Apply(TableColumns columns, string settingsKey)
    {
        var toggled = Toggled(settingsKey);
        var widths = ParseWidths(new AppSettingsRepository().Get(WidthsKey(settingsKey)));

        foreach (var column in columns)
        {
            // Remember the XAML-designed shape before overriding, so "Reset
            // to default" has something to restore.
            column.DefaultWidth = column.Width;
            column.DefaultVisible = column.Visible;

            if (toggled.Contains(column.Header))
                column.Visible = !column.Visible;
            if (widths.TryGetValue(column.Header, out var pixels))
                column.Width = new GridLength(pixels);
        }
    }

    // --- Resized widths -----------------------------------------------------

    /// <summary>Settings key under which a table's user-resized widths live.</summary>
    public static string WidthsKey(string settingsKey) => settingsKey + ".widths";

    /// <summary>
    /// Snapshots every fixed-width column as "header:pixels". Star columns are
    /// left out · they stay proportional, which is what keeps the table filling
    /// its width until the user pins them.
    /// </summary>
    public static void SaveWidths(string settingsKey, TableColumns columns) =>
        new AppSettingsRepository().Set(WidthsKey(settingsKey), string.Join(",",
            columns.Where(c => !c.Width.IsStar)
                   .Select(c => $"{c.Header}:{(int)Math.Round(c.Width.Value)}")));

    /// <summary>
    /// Clears a table's saved choices · hidden columns and resized widths · so
    /// it opens in its designed default from now on. (The live spec is reset by
    /// TableHeader, which calls this.)
    /// </summary>
    public static void Reset(string settingsKey)
    {
        var settings = new AppSettingsRepository();
        settings.Set(settingsKey, string.Empty);
        settings.Set(WidthsKey(settingsKey), string.Empty);
    }

    private static Dictionary<string, double> ParseWidths(string? csv)
    {
        var widths = new Dictionary<string, double>();
        foreach (var entry in (csv ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var split = entry.LastIndexOf(':');
            if (split > 0 && int.TryParse(entry[(split + 1)..], out var pixels) && pixels >= 1)
                widths[entry[..split]] = pixels;
        }
        return widths;
    }
}

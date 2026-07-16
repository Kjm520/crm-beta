using System.Windows;
using Haystack.Data;

namespace Haystack.Views.Common;

/// <summary>
/// Load/save/apply for the list tables' user column choices: which columns are
/// hidden and any widths they've been resized to — both managed live on the
/// tables themselves via the header's right-click menu (see TableHeader), and
/// applied back to a view's column spec at construction, before first layout,
/// so a table only ever renders in its chosen shape.
///
/// Stored per table in app_settings as CSVs — hidden headers, and
/// "header:pixels" width overrides — keyed by header text rather than index, so
/// a saved choice survives columns being added or reordered in code later.
/// </summary>
public static class ListColumns
{
    public const string ContactsKey  = "columns.contacts";
    public const string CompaniesKey = "columns.companies";
    public const string LeadsKey     = "columns.leads";

    private static HashSet<string> Hidden(string settingsKey) =>
        (new AppSettingsRepository().Get(settingsKey) ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet();

    public static void SaveHidden(string settingsKey, IEnumerable<string> hidden) =>
        new AppSettingsRepository().Set(settingsKey, string.Join(",", hidden));

    /// <summary>
    /// Applies the saved choices (hidden columns and resized widths) to a list
    /// view's column spec. Called from the view's constructor, before first
    /// layout, so a table never flashes its default shape.
    /// </summary>
    public static void Apply(TableColumns columns, string settingsKey)
    {
        var hidden = Hidden(settingsKey);
        var widths = ParseWidths(new AppSettingsRepository().Get(WidthsKey(settingsKey)));

        foreach (var column in columns)
        {
            // Remember the XAML-designed width before overriding, so "Reset
            // column widths" has something to restore.
            column.DefaultWidth = column.Width;

            if (hidden.Contains(column.Header))
                column.Visible = false;
            if (widths.TryGetValue(column.Header, out var pixels))
                column.Width = new GridLength(pixels);
        }
    }

    // --- Resized widths -----------------------------------------------------

    /// <summary>Settings key under which a table's user-resized widths live.</summary>
    public static string WidthsKey(string settingsKey) => settingsKey + ".widths";

    /// <summary>
    /// Snapshots every fixed-width column as "header:pixels". Star columns are
    /// left out — they stay proportional, which is what keeps the table filling
    /// its width until the user pins them.
    /// </summary>
    public static void SaveWidths(string settingsKey, TableColumns columns) =>
        new AppSettingsRepository().Set(WidthsKey(settingsKey), string.Join(",",
            columns.Where(c => !c.Width.IsStar)
                   .Select(c => $"{c.Header}:{(int)Math.Round(c.Width.Value)}")));

    /// <summary>
    /// Clears a table's saved choices — hidden columns and resized widths — so
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

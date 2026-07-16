using System.Windows;

namespace Haystack.Views.Common;

/// <summary>
/// One column of an in-house list table: the header label, its width (pixel or
/// star — Auto is deliberately unsupported so layout stays single-pass), the
/// property the column sorts by, and whether the user has it visible (set from
/// the LIST COLUMNS choices in Settings via <see cref="ListColumns.Apply"/>).
/// </summary>
public class TableColumn
{
    public string Header { get; set; } = string.Empty;
    public GridLength Width { get; set; } = new(1, GridUnitType.Star);
    public string? SortMember { get; set; }
    public bool Visible { get; set; } = true;

    /// <summary>
    /// The width declared in the view's XAML, captured by ListColumns.Apply
    /// before any saved user resize overwrites <see cref="Width"/> — it's what
    /// "Reset column widths" restores.
    /// </summary>
    public GridLength? DefaultWidth { get; set; }

    /// <summary>
    /// The narrowest this column may be made interactively. Computed by
    /// TableHeader from the measured header label (plus cell insets and sort
    /// arrow room), so a header can never be squeezed into an ellipsis by
    /// dragging or auto-fit. 48 is the absolute floor until then.
    /// </summary>
    public double MinWidth { get; set; } = 48;
}

/// <summary>
/// A table's full column spec, declared once per view as a XAML resource and
/// shared by the header panel and every row — one source of truth for the
/// geometry, so header and rows can never disagree.
/// </summary>
public class TableColumns : List<TableColumn>;

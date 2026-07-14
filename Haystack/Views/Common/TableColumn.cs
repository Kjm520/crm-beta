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
}

/// <summary>
/// A table's full column spec, declared once per view as a XAML resource and
/// shared by the header panel and every row — one source of truth for the
/// geometry, so header and rows can never disagree.
/// </summary>
public class TableColumns : List<TableColumn>;

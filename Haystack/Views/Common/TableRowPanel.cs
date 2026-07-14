using System.Windows;
using System.Windows.Controls;

namespace Haystack.Views.Common;

/// <summary>
/// Lays out one table row (or the header row) against a shared
/// <see cref="TableColumns"/> spec: children pair with columns by index, fixed
/// widths are taken first, and the remaining space splits across star columns
/// by factor. Hidden columns get zero width.
///
/// Everything resolves from the width constraint alone, inside a single
/// measure/arrange pass — this panel exists because the DataGrid it replaced
/// finalizes star widths one deferred pass AFTER first render, which flashed
/// mis-laid-out headers on every freshly created view. Here the first rendered
/// frame is, structurally, the final one.
/// </summary>
public class TableRowPanel : Panel
{
    public static readonly DependencyProperty ColumnsProperty =
        DependencyProperty.Register(nameof(Columns), typeof(TableColumns), typeof(TableRowPanel),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public TableColumns? Columns
    {
        get => (TableColumns?)GetValue(ColumnsProperty);
        set => SetValue(ColumnsProperty, value);
    }

    /// <summary>
    /// Change signal for mid-session spec edits (a header resize drag mutates the
    /// shared TableColumns, which no panel can observe by itself). TableHeader
    /// bumps this on the rows host after each change; being an INHERITED attached
    /// property, the new value flows to every TableRowPanel in that subtree —
    /// including rows the virtualizer realizes later — and each one re-measures.
    /// No per-panel event subscriptions, so recycled containers can't leak.
    /// </summary>
    public static readonly DependencyProperty LayoutVersionProperty =
        DependencyProperty.RegisterAttached("LayoutVersion", typeof(int), typeof(TableRowPanel),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.Inherits,
                (d, _) => (d as TableRowPanel)?.InvalidateMeasure()));

    public static void SetLayoutVersion(DependencyObject element, int value) => element.SetValue(LayoutVersionProperty, value);
    public static int GetLayoutVersion(DependencyObject element) => (int)element.GetValue(LayoutVersionProperty);

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Columns is not { } columns) return new Size();

        var widths = ComputeWidths(columns, availableSize.Width);
        double height = 0;
        for (var i = 0; i < InternalChildren.Count; i++)
        {
            var width = i < widths.Length ? widths[i] : 0;
            InternalChildren[i].Measure(new Size(width, availableSize.Height));
            height = Math.Max(height, InternalChildren[i].DesiredSize.Height);
        }

        var total = double.IsInfinity(availableSize.Width) ? widths.Sum() : availableSize.Width;
        return new Size(total, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Columns is not { } columns) return finalSize;

        var widths = ComputeWidths(columns, finalSize.Width);
        double x = 0;
        for (var i = 0; i < InternalChildren.Count; i++)
        {
            var width = i < widths.Length ? widths[i] : 0;
            InternalChildren[i].Arrange(new Rect(x, 0, width, finalSize.Height));
            x += width;
        }
        return finalSize;
    }

    private static double[] ComputeWidths(TableColumns columns, double available)
    {
        var widths = new double[columns.Count];

        double fixedSum = 0, starSum = 0;
        foreach (var column in columns)
        {
            if (!column.Visible) continue;
            if (column.Width.IsStar) starSum += column.Width.Value;
            else fixedSum += column.Width.Value;
        }

        var starUnit = starSum > 0 && !double.IsInfinity(available)
            ? Math.Max(0, available - fixedSum) / starSum
            : 0;

        for (var i = 0; i < columns.Count; i++)
        {
            var column = columns[i];
            widths[i] = !column.Visible ? 0
                : column.Width.IsStar ? column.Width.Value * starUnit
                : column.Width.Value;
        }
        return widths;
    }
}

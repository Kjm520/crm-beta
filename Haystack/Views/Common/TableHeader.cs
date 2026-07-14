using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Haystack.Views.Common;

/// <summary>
/// Builds the clickable header cells for an in-house list table and runs its
/// sorting and column resizing.
///
/// Sorting — clicking a header sorts by the column's SortMember (ascending,
/// then descending on a second click) through the list's collection view; the
/// list ViewModels refill their collections in place, so the sort survives
/// searching and reloads.
///
/// Resizing — every visible column boundary carries an invisible 8px grip that
/// straddles it. Dragging pins the left-hand column at the dragged pixel width
/// (star columns absorb the slack, so shrinking can leave a trailing gap —
/// Explorer's model — while widening is clamped so the row can never overflow
/// the card). Double-clicking a grip auto-fits its column to the widest
/// on-screen content. Widths persist per table next to the hidden-column
/// choices (see ListColumns) and are applied back at view construction.
/// </summary>
public class TableHeader
{
    private const double MinColumnWidth = 48;

    // Breathing room added to measured content on auto-fit (small: the cell
    // styles' 12px gutter margin is already part of what Measure reports), and
    // the width the sort arrow needs beside a header label.
    private const double AutoFitPadding = 4;
    private const double ArrowAllowance = 14;

    private static readonly Geometry ArrowUp = Geometry.Parse("M 0 4 L 4 0 L 8 4");
    private static readonly Geometry ArrowDown = Geometry.Parse("M 0 0 L 4 4 L 8 0");

    private readonly TableRowPanel _headerPanel;
    private readonly TableColumns _columns;
    private readonly ItemsControl _rows;
    private readonly string _settingsKey;

    private readonly List<FrameworkElement> _cells = [];
    private readonly List<TextBlock> _labels = [];
    private readonly List<Path> _arrows = [];

    private TableColumn? _sortedBy;
    private ListSortDirection _direction;

    private int _layoutVersion;

    // In-flight resize drag.
    private TableColumn? _resizeColumn;
    private double _resizeStartX;
    private double _resizeStartWidth;
    private double _resizeMaxWidth;

    public TableHeader(TableRowPanel headerPanel, TableColumns columns, ItemsControl rows, string settingsKey)
    {
        _headerPanel = headerPanel;
        _columns = columns;
        _rows = rows;
        _settingsKey = settingsKey;
        headerPanel.Columns = columns;

        var cellStyle = (Style)Application.Current.FindResource("TableHeaderCell");
        var textStyle = (Style)Application.Current.FindResource("TextCaption");

        // Cells pair with columns by index. Each visible column also gets a grip
        // on its LEFT boundary (resizing the previous visible column); the last
        // visible cell gets one on its right edge so it's resizable too.
        TableColumn? previousVisible = null;
        Grid? lastVisibleCell = null;

        foreach (var column in columns)
        {
            var arrow = new Path
            {
                StrokeThickness = 1.5,
                Margin = new Thickness(6, 1, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Collapsed,
            };
            arrow.SetResourceReference(Shape.StrokeProperty, "TextMutedBrush");
            _arrows.Add(arrow);

            var label = new TextBlock
            {
                Text = column.Header,
                Style = textStyle,
                TextTrimming = TextTrimming.CharacterEllipsis,
            };
            _labels.Add(label);

            // Label in a star column so a narrow header trims with an ellipsis
            // instead of painting into its neighbour; arrow in an auto column.
            // The cell presenter left-aligns content at desired size, so with
            // room to spare the arrow still hugs the label. Right margin is the
            // same 12px inter-column gutter the row cells carry.
            var content = new Grid { Margin = new Thickness(0, 0, 12, 0) };
            content.ColumnDefinitions.Add(new ColumnDefinition());
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetColumn(arrow, 1);
            content.Children.Add(label);
            content.Children.Add(arrow);

            var button = new Button { Style = cellStyle, Content = content };
            var col = column;
            button.Click += (_, _) => SortBy(col);

            var cell = new Grid();
            cell.Children.Add(button);
            if (column.Visible)
            {
                if (previousVisible is not null)
                    cell.Children.Add(MakeGrip(previousVisible, HorizontalAlignment.Left));
                previousVisible = column;
                lastVisibleCell = cell;
            }

            _cells.Add(cell);
            headerPanel.Children.Add(cell);
        }

        if (previousVisible is not null && lastVisibleCell is not null)
            lastVisibleCell.Children.Add(MakeGrip(previousVisible, HorizontalAlignment.Right));
    }

    // --- Sorting ------------------------------------------------------------

    private void SortBy(TableColumn column)
    {
        if (column.SortMember is null) return;

        _direction = _sortedBy == column && _direction == ListSortDirection.Ascending
            ? ListSortDirection.Descending
            : ListSortDirection.Ascending;
        _sortedBy = column;

        if (CollectionViewSource.GetDefaultView(_rows.ItemsSource) is not { } view) return;

        using (view.DeferRefresh())
        {
            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(new SortDescription(column.SortMember, _direction));
        }

        for (var i = 0; i < _columns.Count; i++)
        {
            _arrows[i].Visibility = _columns[i] == column ? Visibility.Visible : Visibility.Collapsed;
            _arrows[i].Data = _direction == ListSortDirection.Ascending ? ArrowUp : ArrowDown;
        }
    }

    // --- Resizing -----------------------------------------------------------

    private Border MakeGrip(TableColumn column, HorizontalAlignment side)
    {
        var grip = new Border
        {
            Width = 8,
            Background = Brushes.Transparent,
            Cursor = Cursors.SizeWE,
            HorizontalAlignment = side,
            // A left grip straddles the boundary by overhanging into the cell
            // before it; being the later sibling, it wins hit-testing there. The
            // right-edge grip (last column only) has no neighbour and stays in.
            Margin = side == HorizontalAlignment.Left ? new Thickness(-4, 0, 0, 0) : default,
        };
        grip.MouseLeftButtonDown += (_, e) => GripDown(grip, column, e);
        grip.MouseMove += (_, e) => GripMove(grip, e);
        grip.MouseLeftButtonUp += (_, e) => GripUp(grip, e);
        grip.LostMouseCapture += (_, _) => EndResize();
        return grip;
    }

    private void GripDown(Border grip, TableColumn column, MouseButtonEventArgs e)
    {
        e.Handled = true;

        if (e.ClickCount == 2)
        {
            AutoFit(column);
            return;
        }

        _resizeColumn = column;
        _resizeStartX = e.GetPosition(_headerPanel).X;
        _resizeStartWidth = _cells[_columns.IndexOf(column)].ActualWidth;
        _resizeMaxWidth = MaxWidthFor(column);
        grip.CaptureMouse();
    }

    private void GripMove(Border grip, MouseEventArgs e)
    {
        if (_resizeColumn is null || !grip.IsMouseCaptured) return;

        var width = Math.Clamp(
            _resizeStartWidth + (e.GetPosition(_headerPanel).X - _resizeStartX),
            MinColumnWidth, _resizeMaxWidth);

        _resizeColumn.Width = new GridLength(width);
        BumpLayout();
    }

    private void GripUp(Border grip, MouseButtonEventArgs e)
    {
        if (_resizeColumn is null) return;
        e.Handled = true;
        grip.ReleaseMouseCapture();   // EndResize runs via LostMouseCapture
    }

    // Also covers the gesture being cut short (Alt+Tab, focus steal), so a drag
    // can never be left dangling half-applied and unsaved.
    private void EndResize()
    {
        if (_resizeColumn is null) return;
        _resizeColumn = null;
        ListColumns.SaveWidths(_settingsKey, _columns);
    }

    /// <summary>
    /// How wide a column may grow right now: it can consume the trailing gap and
    /// squeeze the other star columns down to the minimum, but never push the
    /// row past the panel — the tables have no horizontal scroll.
    /// </summary>
    private double MaxWidthFor(TableColumn column)
    {
        double others = 0;
        foreach (var other in _columns)
        {
            if (!other.Visible || other == column) continue;
            others += other.Width.IsStar ? MinColumnWidth : other.Width.Value;
        }
        return Math.Max(MinColumnWidth, _headerPanel.ActualWidth - others);
    }

    /// <summary>
    /// Sizes a column to its widest content among the header label and the rows
    /// currently realized on screen (virtualized-away rows can't be measured —
    /// the same trade Explorer makes).
    /// </summary>
    private void AutoFit(TableColumn column)
    {
        var index = _columns.IndexOf(column);
        var infinite = new Size(double.PositiveInfinity, double.PositiveInfinity);

        _labels[index].Measure(infinite);
        var width = _labels[index].DesiredSize.Width + ArrowAllowance;

        for (var i = 0; i < _rows.Items.Count; i++)
        {
            if (_rows.ItemContainerGenerator.ContainerFromIndex(i) is not DependencyObject container) continue;
            if (FindRowPanel(container) is not { } panel || index >= panel.Children.Count) continue;

            panel.Children[index].Measure(infinite);
            width = Math.Max(width, panel.Children[index].DesiredSize.Width);
        }

        column.Width = new GridLength(
            Math.Clamp(width + AutoFitPadding, MinColumnWidth, MaxWidthFor(column)));

        // Re-measures everything the infinite passes above touched, too.
        BumpLayout();
        ListColumns.SaveWidths(_settingsKey, _columns);
    }

    private void BumpLayout()
    {
        _headerPanel.InvalidateMeasure();
        TableRowPanel.SetLayoutVersion(_rows, ++_layoutVersion);
    }

    private static TableRowPanel? FindRowPanel(DependencyObject node)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
        {
            var child = VisualTreeHelper.GetChild(node, i);
            if (child is TableRowPanel panel) return panel;
            if (FindRowPanel(child) is { } nested) return nested;
        }
        return null;
    }
}

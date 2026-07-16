using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace Haystack.Views.Common;

/// <summary>
/// Builds the clickable header cells for an in-house list table and runs its
/// sorting, column resizing, and column management.
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
/// on-screen content.
///
/// Column management — right-clicking the header opens a context menu in the
/// convention of Explorer/Excel: a check-glyph toggle per column (applied live;
/// the single-pass table layout makes that flash-free), plus Auto-fit and a
/// Reset to default that restores all columns at their designed widths. All
/// choices persist per table via ListColumns and are applied back at view
/// construction.
/// </summary>
public class TableHeader
{
    private const double MinColumnWidth = 48;

    // Breathing room added to measured content on auto-fit (small: the cell
    // styles' 8px inset margins are already part of what Measure reports), and
    // the width the sort arrow needs beside a header label.
    private const double AutoFitPadding = 4;
    private const double ArrowAllowance = 14;

    // The header content's side margins (8 + 8) — keep in sync with the margin
    // on the content Grid below and the cell styles in Controls.xaml.
    private const double HeaderInsets = 16;

    private static readonly Geometry ArrowUp = Geometry.Parse("M 0 4 L 4 0 L 8 4");
    private static readonly Geometry ArrowDown = Geometry.Parse("M 0 0 L 4 4 L 8 0");
    private static readonly Geometry CheckGlyph = Geometry.Parse("M 0 3.5 L 3 6.5 L 8 0.5");

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
    private double _resizeMinWidth;
    private double _resizeMaxWidth;

    // Header context menu (built on first open, items rebuilt per open/toggle).
    private Popup? _menu;
    private StackPanel? _menuList;

    public TableHeader(TableRowPanel headerPanel, TableColumns columns, ItemsControl rows, string settingsKey)
    {
        _headerPanel = headerPanel;
        _columns = columns;
        _rows = rows;
        _settingsKey = settingsKey;

        headerPanel.Columns = columns;
        // Transparent (not null) so right-clicks land even in the trailing gap
        // past the last column.
        headerPanel.Background = Brushes.Transparent;
        headerPanel.MouseRightButtonUp += (_, e) =>
        {
            OpenMenu();
            e.Handled = true;
        };

        BuildCells();
    }

    /// <summary>
    /// (Re)builds the header cells. Called at construction and again whenever a
    /// column is shown or hidden, because the resize-grip topology (one grip per
    /// VISIBLE boundary) depends on which columns are visible.
    /// </summary>
    private void BuildCells()
    {
        _headerPanel.Children.Clear();
        _cells.Clear();
        _labels.Clear();
        _arrows.Clear();

        var cellStyle = (Style)Application.Current.FindResource("TableHeaderCell");
        var textStyle = (Style)Application.Current.FindResource("TextCaption");

        // Cells pair with columns by index. Each visible column also gets a grip
        // on its LEFT boundary (resizing the previous visible column); the last
        // visible cell gets one on its right edge so it's resizable too.
        TableColumn? previousVisible = null;
        Grid? lastVisibleCell = null;

        foreach (var column in _columns)
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

            // A column may never be squeezed narrower than its own header (plus
            // insets and arrow room) — headers don't get ellipsis-trimmed by a
            // drag or auto-fit. The label trimming above is only the graceful
            // fallback for star columns on an extremely narrow window, which the
            // interactive clamps can't govern.
            label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            column.MinWidth = Math.Max(MinColumnWidth,
                label.DesiredSize.Width + HeaderInsets + ArrowAllowance);

            // Label in a star column so a narrow header trims with an ellipsis
            // instead of painting into its neighbour; arrow in an auto column.
            // The cell presenter left-aligns content at desired size, so with
            // room to spare the arrow still hugs the label. Side margins are the
            // same 8px in-cell insets the row cells carry.
            var content = new Grid { Margin = new Thickness(8, 0, 8, 0) };
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
            _headerPanel.Children.Add(cell);
        }

        if (previousVisible is not null && lastVisibleCell is not null)
            lastVisibleCell.Children.Add(MakeGrip(previousVisible, HorizontalAlignment.Right));

        // A rebuild replaces the arrow elements, so restore the active sort's.
        var sorted = _sortedBy is null ? -1 : _columns.IndexOf(_sortedBy);
        if (sorted >= 0)
        {
            _arrows[sorted].Visibility = Visibility.Visible;
            _arrows[sorted].Data = _direction == ListSortDirection.Ascending ? ArrowUp : ArrowDown;
        }
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

    // --- Column menu (right-click) -------------------------------------------

    private void OpenMenu()
    {
        if (_menu is null)
        {
            _menuList = new StackPanel();

            var border = new Border
            {
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(4),
                MinWidth = 190,
                Margin = new Thickness(0, 0, 8, 8),
                Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 16, ShadowDepth = 3, Opacity = 0.3, Direction = 270 },
                Child = _menuList,
            };
            border.SetResourceReference(Border.BackgroundProperty, "BackgroundPanelBrush");
            border.SetResourceReference(Border.BorderBrushProperty, "BorderSubtleBrush");

            _menu = new Popup
            {
                StaysOpen = false,
                AllowsTransparency = true,
                PlacementTarget = _headerPanel,
                Placement = PlacementMode.MousePoint,
                Child = border,
            };
        }

        RebuildMenuItems();
        _menu.IsOpen = true;
    }

    private void RebuildMenuItems()
    {
        _menuList!.Children.Clear();

        var visibleCount = _columns.Count(c => c.Visible);
        foreach (var column in _columns)
            _menuList.Children.Add(MakeToggleItem(column, lastVisible: column.Visible && visibleCount == 1));

        var separator = new Border { Height = 1, Margin = new Thickness(6, 4, 6, 4) };
        separator.SetResourceReference(Border.BackgroundProperty, "BorderSubtleBrush");
        _menuList.Children.Add(separator);

        _menuList.Children.Add(MakeActionItem("Auto-fit columns", AutoFitAll));
        _menuList.Children.Add(MakeActionItem("Reset to default", ResetToDefault));
    }

    /// <summary>
    /// A checkable menu row in the native convention: a check glyph in a left
    /// gutter when the column is shown, no box. Toggles apply live and keep the
    /// menu open (like Linear/Notion column pickers) so several columns can be
    /// flipped in one visit; clicking away closes it.
    /// </summary>
    private Button MakeToggleItem(TableColumn column, bool lastVisible)
    {
        var check = new Path
        {
            Data = CheckGlyph,
            StrokeThickness = 1.6,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            StrokeLineJoin = PenLineJoin.Round,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(1, 0, 0, 0),
            // Hidden (not Collapsed) so unchecked rows keep the gutter width.
            Visibility = column.Visible ? Visibility.Visible : Visibility.Hidden,
        };
        check.SetResourceReference(Shape.StrokeProperty, "TextPrimaryBrush");

        var label = new TextBlock { Text = TitleCase(column.Header), VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(label, 1);

        var content = new Grid();
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
        content.ColumnDefinitions.Add(new ColumnDefinition());
        content.Children.Add(check);
        content.Children.Add(label);

        var item = new Button
        {
            Style = (Style)Application.Current.FindResource("MenuDropItem"),
            Content = content,
            // The last visible column can't be hidden — an all-blank table with
            // no header left to right-click would be a trap.
            IsEnabled = !lastVisible,
            Opacity = lastVisible ? 0.45 : 1,
        };
        item.Click += (_, _) => ToggleColumn(column);
        return item;
    }

    private Button MakeActionItem(string text, Action action)
    {
        var item = new Button
        {
            Style = (Style)Application.Current.FindResource("MenuDropItem"),
            // Left margin aligns the text with the toggle labels' gutter.
            Content = new TextBlock { Text = text, Margin = new Thickness(20, 0, 0, 0) },
        };
        item.Click += (_, _) =>
        {
            _menu!.IsOpen = false;
            action();
        };
        return item;
    }

    private void ToggleColumn(TableColumn column)
    {
        column.Visible = !column.Visible;

        BuildCells();       // grip topology follows visibility
        BumpLayout();
        ListColumns.SaveHidden(_settingsKey, _columns.Where(c => !c.Visible).Select(c => c.Header));
        RebuildMenuItems(); // refresh check glyphs and the last-column guard
    }

    /// <summary>Menu labels read like Explorer's ("Last Contact"), not like the
    /// uppercase table headers they name.</summary>
    private static string TitleCase(string header)
        => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(header.ToLowerInvariant());

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
            BumpLayout();
            ListColumns.SaveWidths(_settingsKey, _columns);
            return;
        }

        _resizeColumn = column;
        _resizeStartX = e.GetPosition(_headerPanel).X;
        _resizeStartWidth = _cells[_columns.IndexOf(column)].ActualWidth;
        _resizeMinWidth = column.MinWidth;
        _resizeMaxWidth = Math.Max(column.MinWidth, MaxWidthFor(column));
        grip.CaptureMouse();
    }

    private void GripMove(Border grip, MouseEventArgs e)
    {
        if (_resizeColumn is null || !grip.IsMouseCaptured) return;

        var width = Math.Clamp(
            _resizeStartWidth + (e.GetPosition(_headerPanel).X - _resizeStartX),
            _resizeMinWidth, _resizeMaxWidth);

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
    /// squeeze the other star columns down to their own minimums (each protects
    /// its header text), but never push the row past the panel — the tables have
    /// no horizontal scroll.
    /// </summary>
    private double MaxWidthFor(TableColumn column)
    {
        double others = 0;
        foreach (var other in _columns)
        {
            if (!other.Visible || other == column) continue;
            others += other.Width.IsStar ? other.MinWidth : other.Width.Value;
        }
        return Math.Max(column.MinWidth, _headerPanel.ActualWidth - others);
    }

    /// <summary>
    /// Returns the table to its designed default, live: every column shown,
    /// each at its XAML-designed width.
    /// </summary>
    private void ResetToDefault()
    {
        foreach (var column in _columns)
        {
            column.Visible = true;
            if (column.DefaultWidth is { } design)
                column.Width = design;
        }

        BuildCells();   // visibility may have changed, so grip topology follows
        BumpLayout();
        ListColumns.Reset(_settingsKey);
    }

    private void AutoFitAll()
    {
        foreach (var column in _columns)
            if (column.Visible)
                AutoFit(column);

        BumpLayout();
        ListColumns.SaveWidths(_settingsKey, _columns);
    }

    /// <summary>
    /// Sizes a column to its widest content among the header label and the rows
    /// currently realized on screen (virtualized-away rows can't be measured —
    /// the same trade Explorer makes). Caller bumps layout and saves.
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
            Math.Clamp(width + AutoFitPadding, column.MinWidth,
                Math.Max(column.MinWidth, MaxWidthFor(column))));
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

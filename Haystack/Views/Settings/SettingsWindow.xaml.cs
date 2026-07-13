using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Haystack.Models;
using Haystack.Navigation;
using Haystack.ViewModels;
using Haystack.Views.Dialogs;

namespace Haystack.Views.Settings;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _vm = new();

    public SettingsWindow()
    {
        InitializeComponent();
        DataContext = _vm;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        => DragMove();

    // --- Nav tab drag-to-reorder ------------------------------------------
    //
    // A live, mouse-capture drag rather than WPF's DragDrop.DoDragDrop. DoDragDrop
    // runs its own blocking loop and reorders only on drop, so the list sat frozen
    // for the whole gesture — the grip looked broken. Here the grabbed row follows
    // the cursor on a TranslateTransform, and every row it crosses is reordered in
    // the collection immediately and animated into its new slot, so the list shows
    // the result continuously. Nothing is persisted until the drag settles.

    private static readonly Duration ReflowDuration =
        new(TimeSpan.FromMilliseconds(140));

    private static IEasingFunction ReflowEase =>
        new QuadraticEase { EasingMode = EasingMode.EaseOut };

    private NavItem? _navDragItem;          // the item being dragged
    private FrameworkElement? _navDragRow;  // ...and its generated container
    private double _navDragStartY;          // cursor Y (in list space) when grabbed
    private double _navDragHomeShift;       // how far the row's own slot has moved under it

    private void NavHandle_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: NavItem item }) return;
        if (NavList.ItemContainerGenerator.ContainerFromItem(item) is not FrameworkElement row) return;

        _navDragItem = item;
        _navDragRow = row;
        _navDragStartY = e.GetPosition(NavList).Y;
        _navDragHomeShift = 0;

        // Lift the row above its neighbours so it slides over them, not under.
        row.RenderTransform = new TranslateTransform();
        Panel.SetZIndex(row, 10);
        row.Opacity = 0.85;

        NavList.CaptureMouse();
        e.Handled = true;
    }

    private void NavList_MouseMove(object sender, MouseEventArgs e)
    {
        if (_navDragItem is null || _navDragRow is null) return;
        if (e.LeftButton != MouseButtonState.Pressed) { EndNavDrag(); return; }

        var cursorY = e.GetPosition(NavList).Y;
        var offset = cursorY - _navDragStartY + _navDragHomeShift;
        ((TranslateTransform)_navDragRow.RenderTransform).Y = offset;

        // Reorder as soon as the dragged row's centre enters another row's band.
        var from = _vm.NavItems.IndexOf(_navDragItem);
        var centre = SlotTops()[_navDragItem] + _navDragRow.ActualHeight / 2 + offset;
        var to = SlotIndexAt(centre);
        if (to == from || to < 0) return;

        // Snapshot where every row sits, reorder, then animate each displaced row
        // from where it *was* back to zero — that's what makes them appear to slide.
        var before = SlotTops();
        _vm.MoveNav(from, to);
        NavList.UpdateLayout();
        var after = SlotTops();

        foreach (var (item, oldTop) in before)
        {
            if (!after.TryGetValue(item, out var newTop)) continue;
            var shift = oldTop - newTop;

            // The dragged row isn't animated — it's pinned to the cursor. Its slot
            // moved out from under it, so compensate to keep it visually still.
            if (item == _navDragItem)
            {
                _navDragHomeShift += shift;
                ((TranslateTransform)_navDragRow.RenderTransform).Y =
                    cursorY - _navDragStartY + _navDragHomeShift;
                continue;
            }

            if (Math.Abs(shift) < 0.5) continue;
            if (NavList.ItemContainerGenerator.ContainerFromItem(item) is not FrameworkElement row) continue;

            var slide = EnsureTranslate(row);
            slide.BeginAnimation(TranslateTransform.YProperty, null);
            slide.Y = shift;
            slide.BeginAnimation(TranslateTransform.YProperty,
                new DoubleAnimation(0, ReflowDuration) { EasingFunction = ReflowEase });
        }
    }

    private void NavList_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) => EndNavDrag();

    // Covers the gesture being cut short (Alt+Tab, a modal stealing focus, …) so a
    // half-dragged row can't be left floating.
    private void NavList_LostMouseCapture(object sender, MouseEventArgs e) => EndNavDrag();

    private void EndNavDrag()
    {
        if (_navDragItem is null || _navDragRow is null) return;

        // Settle the row into its final slot rather than snapping.
        var row = _navDragRow;
        var settle = new DoubleAnimation(0, ReflowDuration) { EasingFunction = ReflowEase };
        settle.Completed += (_, _) =>
        {
            Panel.SetZIndex(row, 0);
            row.Opacity = 1;
        };
        EnsureTranslate(row).BeginAnimation(TranslateTransform.YProperty, settle);

        _vm.CommitNavOrder();

        _navDragItem = null;
        _navDragRow = null;
        if (NavList.IsMouseCaptured) NavList.ReleaseMouseCapture();
    }

    private static TranslateTransform EnsureTranslate(FrameworkElement row)
    {
        if (row.RenderTransform is TranslateTransform existing) return existing;
        var created = new TranslateTransform();
        row.RenderTransform = created;
        return created;
    }

    /// <summary>
    /// Each item's laid-out top edge, by stacking row heights. Deliberately measured
    /// from the layout rather than TransformToAncestor, which would fold in the
    /// RenderTransforms this drag is applying and feed them back into itself.
    /// </summary>
    private Dictionary<NavItem, double> SlotTops()
    {
        var tops = new Dictionary<NavItem, double>();
        double y = 0;
        for (var i = 0; i < _vm.NavItems.Count; i++)
        {
            if (NavList.ItemContainerGenerator.ContainerFromIndex(i) is not FrameworkElement row) continue;
            tops[_vm.NavItems[i]] = y;
            y += row.ActualHeight;
        }
        return tops;
    }

    /// <summary>The index of the row whose band contains <paramref name="y"/> (clamped to the list).</summary>
    private int SlotIndexAt(double y)
    {
        double top = 0;
        for (var i = 0; i < _vm.NavItems.Count; i++)
        {
            if (NavList.ItemContainerGenerator.ContainerFromIndex(i) is not FrameworkElement row) continue;
            if (y < top + row.ActualHeight) return y < 0 ? 0 : i;
            top += row.ActualHeight;
        }
        return _vm.NavItems.Count - 1;
    }

    private void AddStatus_Click(object sender, RoutedEventArgs e)
    {
        var problem = _vm.AddStatus();
        if (problem is not null)
            ConfirmWindow.Alert(this, "Can't add status", problem);
    }

    private void RenameStatus_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: LeadStatus status }) return;

        var prompt = new TextPromptWindow("Rename status", "New name:", status.Name) { Owner = this };
        if (prompt.ShowDialog() != true) return;

        var problem = _vm.RenameStatus(status, prompt.Value);
        if (problem is not null)
            ConfirmWindow.Alert(this, "Can't rename status", problem);
    }

    private void DeleteStatus_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: LeadStatus status }) return;

        var message = status.LeadCount > 0
            ? $"{status.LeadCount} lead(s) use this status. They will be kept, but become unassigned."
            : "No leads use this status.";

        if (ConfirmWindow.Ask(this, "Delete status", status.Name, message))
            _vm.DeleteStatusCommand.Execute(status);
    }

    private void Close_Click(object sender, RoutedEventArgs e)
        => Close();
}

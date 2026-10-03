using System.Windows;
using System.Windows.Controls;

namespace Haystack.Views.Common;

/// <summary>
/// A vertical stack that shows only the children that fully fit its height and
/// parks the rest · no scrollbar, no clipped half-rows. Built for dashboard
/// cards: a dashboard list is a glance, not an archive, so it simply shows as
/// many entries as the card has room for at the current window size. Children
/// keep their order; the stack stops at the first one that doesn't fit.
/// </summary>
public class FitStackPanel : Panel
{
    protected override Size MeasureOverride(Size availableSize)
    {
        double natural = 0;
        foreach (UIElement child in InternalChildren)
        {
            child.Measure(new Size(availableSize.Width, double.PositiveInfinity));
            natural += child.DesiredSize.Height;
        }

        return new Size(
            double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width,
            double.IsInfinity(availableSize.Height) ? natural : availableSize.Height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        double y = 0;
        var fits = true;

        foreach (UIElement child in InternalChildren)
        {
            var height = child.DesiredSize.Height;
            if (fits && y + height <= finalSize.Height + 0.5)
            {
                child.Arrange(new Rect(0, y, finalSize.Width, height));
                y += height;
            }
            else
            {
                fits = false;
                child.Arrange(new Rect(0, 0, 0, 0));
            }
        }
        return finalSize;
    }
}

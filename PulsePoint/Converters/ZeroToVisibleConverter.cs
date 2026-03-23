using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PulsePoint.Converters;

/// <summary>
/// Returns Visible when the integer value is zero; Collapsed otherwise.
/// Used to show "empty state" messages when a list has no items.
/// </summary>
[ValueConversion(typeof(int), typeof(Visibility))]
public class ZeroToVisibleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is int count && count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

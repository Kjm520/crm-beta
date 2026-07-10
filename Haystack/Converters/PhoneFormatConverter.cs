using System.Globalization;
using System.Windows.Data;
using Haystack.Models;

namespace Haystack.Converters;

/// <summary>
/// Displays a stored phone number as xxx-xxx-xxxx. Two-way so it also normalizes
/// what the user types back into storage (digits only for US numbers).
/// </summary>
[ValueConversion(typeof(string), typeof(string))]
public class PhoneFormatConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => PhoneNumber.Format(value as string);

    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => PhoneNumber.Normalize(value as string);
}

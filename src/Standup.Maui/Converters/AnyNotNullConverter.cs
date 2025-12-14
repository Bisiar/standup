using System.Globalization;

namespace Standup.Maui.Converters;

/// <summary>
/// Returns true when any input value in a MultiBinding is not null.
/// </summary>
public class AnyNotNullConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        return values.Any(v => v != null);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

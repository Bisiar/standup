using System.Globalization;

namespace Standup.Maui.Converters;

/// <summary>
/// Returns true when all input values in a MultiBinding are null.
/// </summary>
public class BothNullConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        return values.All(v => v == null);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

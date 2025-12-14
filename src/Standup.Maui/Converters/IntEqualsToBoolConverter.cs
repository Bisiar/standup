using System.Globalization;

namespace Standup.Maui.Converters;

/// <summary>
/// Converts int to bool by comparing against a parameter value.
/// Usage: IsVisible="{Binding SelectedTabIndex, Converter={StaticResource IntEqualsToBoolConverter}, ConverterParameter=0}".
/// </summary>
public class IntEqualsToBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int index && parameter is string paramStr && int.TryParse(paramStr, out int targetIndex))
        {
            return index == targetIndex;
        }

        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

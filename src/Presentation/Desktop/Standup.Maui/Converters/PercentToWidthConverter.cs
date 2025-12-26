using System.Globalization;

namespace Standup.Maui.Converters;

/// <summary>
/// Converts a percentage value (0-100) to a width for progress bars.
/// ConverterParameter specifies the maximum width (default 200).
/// </summary>
public class PercentToWidthConverter : IValueConverter
{
    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not double percent)
        {
            return 0;
        }

        var maxWidth = 200.0;
        if (parameter is string paramStr && double.TryParse(paramStr, out var parsed))
        {
            maxWidth = parsed;
        }
        else if (parameter is double paramDouble)
        {
            maxWidth = paramDouble;
        }

        return Math.Max(0, Math.Min(maxWidth, percent / 100.0 * maxWidth));
    }

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

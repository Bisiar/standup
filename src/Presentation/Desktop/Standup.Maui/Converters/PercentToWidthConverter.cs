using System.Globalization;

namespace Standup.Maui.Converters;

/// <summary>
/// Converts percentage values to AbsoluteLayout bounds for stacked bar charts.
/// Uses MultiBinding with AdditionsPercent and DeletionsPercent.
/// ConverterParameter: "additions" starts at x=0, "deletions" starts after additions.
/// </summary>
public class PercentToWidthConverter : IMultiValueConverter
{
    /// <inheritdoc/>
    public object Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        // MultiBinding: values[0] = AdditionsPercent, values[1] = DeletionsPercent
        if (values.Length < 2)
        {
            return new Rect(0, 0, 0, 1);
        }

        var additionsPercent = values[0] as double? ?? 0;
        var deletionsPercent = values[1] as double? ?? 0;
        var paramStr = parameter as string ?? "additions";

        var additionsWidth = additionsPercent / 100.0;
        var deletionsWidth = deletionsPercent / 100.0;

        if (paramStr == "additions")
        {
            // Additions: start at 0, width = additions percentage
            return new Rect(0, 0, additionsWidth, 1);
        }
        else
        {
            // Deletions: start after additions, width = deletions percentage
            return new Rect(additionsWidth, 0, deletionsWidth, 1);
        }
    }

    /// <inheritdoc/>
    public object?[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

// <copyright file="NullToColorConverter.cs" company="Bisiar">
// Copyright (c) Bisiar. All rights reserved.
// </copyright>

using System.Globalization;

namespace Standup.Maui.Converters;

/// <summary>
/// Converts null value to color (amber if null, green if has value).
/// </summary>
public class NullToColorConverter : IValueConverter
{
    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (string.IsNullOrEmpty(value as string))
        {
            return Color.FromArgb("#fbbf24");
        }

        return Color.FromArgb("#34d399");
    }

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

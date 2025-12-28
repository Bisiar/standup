// <copyright file="NullToTextConverter.cs" company="Bisiar">
// Copyright (c) Bisiar. All rights reserved.
// </copyright>

using System.Globalization;

namespace Standup.Maui.Converters;

/// <summary>
/// Converts null value to fallback text (parameter).
/// </summary>
public class NullToTextConverter : IValueConverter
{
    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (string.IsNullOrEmpty(value as string))
        {
            return parameter as string ?? "—";
        }

        return value;
    }

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

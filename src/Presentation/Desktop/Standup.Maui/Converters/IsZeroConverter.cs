// <copyright file="IsZeroConverter.cs" company="Bisiar">
// Copyright (c) Bisiar. All rights reserved.
// </copyright>

using System.Globalization;

namespace Standup.Maui.Converters;

/// <summary>
/// Converts count to bool (true if zero).
/// </summary>
public class IsZeroConverter : IValueConverter
{
    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            return count == 0;
        }

        return true;
    }

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

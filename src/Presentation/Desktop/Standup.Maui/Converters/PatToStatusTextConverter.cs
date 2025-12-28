// <copyright file="PatToStatusTextConverter.cs" company="Bisiar">
// Copyright (c) Bisiar. All rights reserved.
// </copyright>

using System.Globalization;

namespace Standup.Maui.Converters;

/// <summary>
/// Converts PAT value to status text.
/// </summary>
public class PatToStatusTextConverter : IValueConverter
{
    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var hasPat = !string.IsNullOrEmpty(value as string);
        return hasPat ? "✓ PAT" : "⚠ No PAT";
    }

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

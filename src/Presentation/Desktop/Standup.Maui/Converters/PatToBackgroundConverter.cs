// <copyright file="PatToBackgroundConverter.cs" company="Bisiar">
// Copyright (c) Bisiar. All rights reserved.
// </copyright>

using System.Globalization;

namespace Standup.Maui.Converters;

/// <summary>
/// Converts PAT value to status badge background color.
/// </summary>
public class PatToBackgroundConverter : IValueConverter
{
    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var hasPat = !string.IsNullOrEmpty(value as string);
        return hasPat ? Color.FromArgb("#065f46") : Color.FromArgb("#78350f");
    }

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

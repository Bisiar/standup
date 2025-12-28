// <copyright file="PatToTextColorConverter.cs" company="Bisiar">
// Copyright (c) Bisiar. All rights reserved.
// </copyright>

using System.Globalization;

namespace Standup.Maui.Converters;

/// <summary>
/// Converts PAT value to status text color.
/// </summary>
public class PatToTextColorConverter : IValueConverter
{
    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var hasPat = !string.IsNullOrEmpty(value as string);
        return hasPat ? Color.FromArgb("#34d399") : Color.FromArgb("#fbbf24");
    }

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

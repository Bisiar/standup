// <copyright file="DateToRelativeConverter.cs" company="Bisiar">
// Copyright (c) Bisiar. All rights reserved.
// </copyright>

using System.Globalization;

namespace Standup.Maui.Converters;

/// <summary>
/// Converts a DateTimeOffset to a relative time string.
/// </summary>
public class DateToRelativeConverter : IValueConverter
{
    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTimeOffset timestamp)
        {
            var diff = DateTimeOffset.UtcNow - timestamp;
            if (diff.TotalMinutes < 60)
            {
                return $"{(int)diff.TotalMinutes}m ago";
            }

            if (diff.TotalHours < 24)
            {
                return $"{(int)diff.TotalHours}h ago";
            }

            if (diff.TotalDays < 7)
            {
                return $"{(int)diff.TotalDays}d ago";
            }

            return timestamp.ToString("MMM dd");
        }

        return "—";
    }

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

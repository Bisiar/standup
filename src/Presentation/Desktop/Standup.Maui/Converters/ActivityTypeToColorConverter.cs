// <copyright file="ActivityTypeToColorConverter.cs" company="Bisiar">
// Copyright (c) Bisiar. All rights reserved.
// </copyright>

using System.Globalization;
using Standup.Application.Models;

namespace Standup.Maui.Converters;

/// <summary>
/// Converts ActivityType to dot color.
/// </summary>
public class ActivityTypeToColorConverter : IValueConverter
{
    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ActivityType type)
        {
            return type switch
            {
                ActivityType.Commit => Color.FromArgb("#3b82f6"),
                ActivityType.PullRequest => Color.FromArgb("#8b5cf6"),
                ActivityType.Task => Color.FromArgb("#22c55e"),
                _ => Color.FromArgb("#64748b")
            };
        }

        return Color.FromArgb("#64748b");
    }

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

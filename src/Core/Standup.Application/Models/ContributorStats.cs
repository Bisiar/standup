// -----------------------------------------------------------------------
// <copyright file="ContributorStats.cs" company="JourneyTeam">
// Copyright (c) JourneyTeam. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Standup.Application.Models;

/// <summary>
/// Represents commit statistics for a contributor on the dashboard.
/// </summary>
public class ContributorStats
{
    /// <summary>
    /// Gets or sets the contributor's display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the contributor's email (used as unique identifier).
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of commits by this contributor.
    /// </summary>
    public int CommitCount { get; set; }

    /// <summary>
    /// Gets or sets the total lines added by this contributor.
    /// </summary>
    public int Additions { get; set; }

    /// <summary>
    /// Gets or sets the total lines deleted by this contributor.
    /// </summary>
    public int Deletions { get; set; }

    /// <summary>
    /// Gets the total lines changed (additions + deletions).
    /// </summary>
    public int TotalChanges => Additions + Deletions;

    /// <summary>
    /// Gets the contributor's initials for avatar display.
    /// </summary>
    public string Initials
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                return "?";
            }

            var parts = Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
            }

            return Name.Length >= 2 ? Name[..2].ToUpperInvariant() : Name.ToUpperInvariant();
        }
    }

    /// <summary>
    /// Gets a color for this contributor based on their name hash (for avatar background).
    /// </summary>
    public string AvatarColor
    {
        get
        {
            var hash = Math.Abs(Name.GetHashCode());
            var colors = new[]
            {
                "#3B82F6", // Blue
                "#10B981", // Green
                "#F59E0B", // Amber
                "#EF4444", // Red
                "#8B5CF6", // Purple
                "#EC4899", // Pink
                "#06B6D4", // Cyan
                "#84CC16", // Lime
            };
            return colors[hash % colors.Length];
        }
    }
}

// <copyright file="GroupStats.cs" company="Bisiar">
// Copyright (c) Bisiar. All rights reserved.
// </copyright>

namespace Standup.Application.Models;

/// <summary>
/// Statistics for a selected group shown in the detail panel.
/// </summary>
public record GroupStats(
    int RepositoryCount,
    int CommitCount7d,
    int ReposWithPat)
{
    /// <summary>
    /// Gets an empty stats object with all zeros.
    /// </summary>
    public static GroupStats Empty => new(0, 0, 0);
}

// <copyright file="ProjectStats.cs" company="Bisiar">
// Copyright (c) Bisiar. All rights reserved.
// </copyright>

namespace Standup.Application.Models;

/// <summary>
/// Statistics for a selected project shown in the detail panel.
/// </summary>
public record ProjectStats(
    int CommitCount30d,
    int TasksDone,
    int InProgress,
    int OpenPRs)
{
    /// <summary>
    /// Gets an empty stats object with all zeros.
    /// </summary>
    public static ProjectStats Empty => new(0, 0, 0, 0);
}

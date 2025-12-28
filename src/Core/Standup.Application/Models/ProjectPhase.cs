// <copyright file="ProjectPhase.cs" company="Standup">
// Copyright (c) Standup. All rights reserved.
// </copyright>

namespace Standup.Application.Models;

/// <summary>
/// Represents a project phase in the timeline.
/// </summary>
/// <param name="Name">Phase name.</param>
/// <param name="Dates">Date range string.</param>
/// <param name="Status">Phase status.</param>
public record ProjectPhase(string Name, string Dates, PhaseStatus Status)
{
    /// <summary>
    /// Gets a value indicating whether the phase is complete.
    /// </summary>
    public bool IsComplete => Status == PhaseStatus.Complete;

    /// <summary>
    /// Gets a value indicating whether the phase is in progress.
    /// </summary>
    public bool IsInProgress => Status == PhaseStatus.InProgress;

    /// <summary>
    /// Gets a value indicating whether the phase is pending.
    /// </summary>
    public bool IsPending => Status == PhaseStatus.Pending;

    /// <summary>
    /// Gets the status text.
    /// </summary>
    public string StatusText => Status switch
    {
        PhaseStatus.Complete => "Complete",
        PhaseStatus.InProgress => "In Progress",
        PhaseStatus.Pending => "Pending",
        _ => "Unknown",
    };
}

/// <summary>
/// Phase status enumeration.
/// </summary>
public enum PhaseStatus
{
    /// <summary>Pending phase.</summary>
    Pending,

    /// <summary>In progress phase.</summary>
    InProgress,

    /// <summary>Complete phase.</summary>
    Complete,
}

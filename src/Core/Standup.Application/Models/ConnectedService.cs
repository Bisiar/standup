// <copyright file="ConnectedService.cs" company="Standup">
// Copyright (c) Standup. All rights reserved.
// </copyright>

namespace Standup.Application.Models;

/// <summary>
/// Represents a connected service.
/// </summary>
public record ConnectedService(string Name, string Icon, ServiceStatus Status)
{
    /// <summary>
    /// Gets the status text.
    /// </summary>
    public string StatusText => Status switch
    {
        ServiceStatus.Connected => "Connected",
        ServiceStatus.Syncing => "Syncing",
        ServiceStatus.Error => "Error",
        _ => "Unknown",
    };

    /// <summary>
    /// Gets the status background color.
    /// </summary>
    public string StatusBackground => Status switch
    {
        ServiceStatus.Connected => "#065F46",
        ServiceStatus.Syncing => "#1E3A5F",
        ServiceStatus.Error => "#7F1D1D",
        _ => "#334155",
    };

    /// <summary>
    /// Gets the status text color.
    /// </summary>
    public string StatusTextColor => Status switch
    {
        ServiceStatus.Connected => "#34D399",
        ServiceStatus.Syncing => "#60A5FA",
        ServiceStatus.Error => "#FCA5A5",
        _ => "#94A3B8",
    };
}

/// <summary>
/// Service status enumeration.
/// </summary>
public enum ServiceStatus
{
    /// <summary>Connected status.</summary>
    Connected,

    /// <summary>Syncing status.</summary>
    Syncing,

    /// <summary>Error status.</summary>
    Error,
}

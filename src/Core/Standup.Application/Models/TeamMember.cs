// <copyright file="TeamMember.cs" company="Standup">
// Copyright (c) Standup. All rights reserved.
// </copyright>

namespace Standup.Application.Models;

/// <summary>
/// Represents a team member.
/// </summary>
public record TeamMember(string Name, string Initials, string Role, TeamRole RoleType, int TaskCount, MemberStatus Status)
{
    /// <summary>
    /// Gets the avatar color based on role.
    /// </summary>
    public string AvatarColor => RoleType switch
    {
        TeamRole.Lead => "#3B82F6",
        TeamRole.Developer => "#10B981",
        TeamRole.QA => "#F59E0B",
        TeamRole.DevOps => "#8B5CF6",
        _ => "#64748B",
    };

    /// <summary>
    /// Gets the status dot color.
    /// </summary>
    public string StatusColor => Status switch
    {
        MemberStatus.Active => "#22C55E",
        MemberStatus.Away => "#F59E0B",
        MemberStatus.Offline => "#64748B",
        _ => "#64748B",
    };

    /// <summary>
    /// Gets the status text.
    /// </summary>
    public string StatusText => Status.ToString();
}

/// <summary>
/// Team role enumeration.
/// </summary>
public enum TeamRole
{
    /// <summary>Lead role.</summary>
    Lead,

    /// <summary>Developer role.</summary>
    Developer,

    /// <summary>QA role.</summary>
    QA,

    /// <summary>DevOps role.</summary>
    DevOps,
}

/// <summary>
/// Member status enumeration.
/// </summary>
public enum MemberStatus
{
    /// <summary>Active status.</summary>
    Active,

    /// <summary>Away status.</summary>
    Away,

    /// <summary>Offline status.</summary>
    Offline,
}

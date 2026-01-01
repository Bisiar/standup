// <copyright file="CrmDataResult.cs" company="Standup">
// Copyright (c) Standup. All rights reserved.
// </copyright>

using Standup.Domain.Entities;

namespace Standup.Application.ViewModels;

/// <summary>
/// Result from loading CRM data from a specific tenant.
/// </summary>
/// <param name="Project">The CRM project details.</param>
/// <param name="Milestones">The project milestones.</param>
/// <param name="Tasks">The in-progress project tasks.</param>
public record CrmDataResult(CrmProject? Project, IReadOnlyList<CrmMilestone> Milestones, IReadOnlyList<CrmTask> Tasks);

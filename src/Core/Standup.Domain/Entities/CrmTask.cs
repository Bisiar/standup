// <copyright file="CrmTask.cs" company="Standup">
// Copyright (c) Standup. All rights reserved.
// </copyright>

namespace Standup.Domain.Entities;

/// <summary>
/// Represents a task from Dynamics 365 Project Operations (msdyn_projecttask).
/// </summary>
/// <param name="TaskId">The unique identifier of the task.</param>
/// <param name="Name">The task name/subject.</param>
/// <param name="Description">The task description.</param>
/// <param name="ScheduledEnd">The scheduled end date.</param>
/// <param name="Progress">The progress percentage (0-100).</param>
/// <param name="AssignedTo">The name of the assigned resource.</param>
/// <param name="IsActive">Whether the task is active (not completed or cancelled).</param>
public record CrmTask(
    string TaskId,
    string Name,
    string? Description,
    DateTime? ScheduledEnd,
    decimal Progress,
    string? AssignedTo,
    bool IsActive);

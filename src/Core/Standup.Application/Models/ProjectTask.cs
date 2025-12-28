// <copyright file="ProjectTask.cs" company="Standup">
// Copyright (c) Standup. All rights reserved.
// </copyright>

namespace Standup.Application.Models;

/// <summary>
/// Represents a project task.
/// </summary>
public class ProjectTask
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectTask"/> class.
    /// </summary>
    /// <param name="id">Task ID.</param>
    /// <param name="title">Task title.</param>
    /// <param name="assigneeInitials">Assignee initials.</param>
    /// <param name="assigneeName">Assignee name.</param>
    /// <param name="priority">Task priority.</param>
    /// <param name="dueDate">Due date.</param>
    public ProjectTask(string id, string title, string assigneeInitials, string assigneeName, TaskPriority priority, DateTimeOffset dueDate)
    {
        Id = id;
        Title = title;
        AssigneeInitials = assigneeInitials;
        AssigneeName = assigneeName;
        Priority = priority;
        DueDate = dueDate;
    }

    /// <summary>Gets or sets the task ID.</summary>
    public string Id { get; set; }

    /// <summary>Gets or sets the task title.</summary>
    public string Title { get; set; }

    /// <summary>Gets or sets the assignee initials.</summary>
    public string AssigneeInitials { get; set; }

    /// <summary>Gets or sets the assignee name.</summary>
    public string AssigneeName { get; set; }

    /// <summary>Gets or sets the priority.</summary>
    public TaskPriority Priority { get; set; }

    /// <summary>Gets or sets the due date.</summary>
    public DateTimeOffset DueDate { get; set; }

    /// <summary>Gets or sets a value indicating whether the task is overdue.</summary>
    public bool IsOverdue { get; set; }

    /// <summary>Gets the priority text.</summary>
    public string PriorityText => Priority.ToString();

    /// <summary>Gets the priority color.</summary>
    public string PriorityColor => Priority switch
    {
        TaskPriority.High => "#EF4444",
        TaskPriority.Medium => "#F59E0B",
        TaskPriority.Low => "#22C55E",
        _ => "#64748B",
    };

    /// <summary>Gets the priority background color.</summary>
    public string PriorityBackground => Priority switch
    {
        TaskPriority.High => "#7F1D1D",
        TaskPriority.Medium => "#78350F",
        TaskPriority.Low => "#14532D",
        _ => "#334155",
    };

    /// <summary>Gets the due date display text.</summary>
    public string DueDateText => IsOverdue ? $"⚠️ {DueDate:MMM dd}" : $"📅 {DueDate:MMM dd}";
}

/// <summary>
/// Task priority enumeration.
/// </summary>
public enum TaskPriority
{
    /// <summary>Low priority.</summary>
    Low,

    /// <summary>Medium priority.</summary>
    Medium,

    /// <summary>High priority.</summary>
    High,
}

// <copyright file="ProjectGroup.cs" company="Bisiar">
// Copyright (c) Bisiar. All rights reserved.
// </copyright>

using System.Collections.ObjectModel;

namespace Standup.Application.Models;

/// <summary>
/// Represents a group of projects organized by organization/tenant.
/// Used for grouped display in the master-detail UI.
/// </summary>
public class ProjectGroup : ObservableCollection<ProjectInstance>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectGroup"/> class.
    /// </summary>
    /// <param name="name">The group name (organization/tenant).</param>
    /// <param name="projects">The projects in this group.</param>
    public ProjectGroup(string name, IEnumerable<ProjectInstance> projects)
        : base(projects)
    {
        Name = name;
    }

    /// <summary>
    /// Gets the group name (typically the organization or tenant name).
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the count of projects in this group.
    /// </summary>
    public new int Count => base.Count;
}

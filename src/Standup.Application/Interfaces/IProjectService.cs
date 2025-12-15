using Standup.Application.Models;

namespace Standup.Application.Interfaces;

/// <summary>
/// Service interface for managing project instances.
/// </summary>
public interface IProjectService
{
    /// <summary>
    /// Gets all configured projects.
    /// </summary>
    /// <returns>List of project instances.</returns>
    Task<IEnumerable<ProjectInstance>> GetProjectsAsync();

    /// <summary>
    /// Gets the currently active project.
    /// </summary>
    /// <returns>The current project, or null if none is set.</returns>
    Task<ProjectInstance?> GetCurrentProjectAsync();

    /// <summary>
    /// Adds a new project.
    /// </summary>
    /// <param name="project">The project to add.</param>
    /// <returns>The added project.</returns>
    Task<ProjectInstance> AddProjectAsync(ProjectInstance project);

    /// <summary>
    /// Updates an existing project.
    /// </summary>
    /// <param name="project">The project with updated values.</param>
    /// <returns>The updated project.</returns>
    Task<ProjectInstance> UpdateProjectAsync(ProjectInstance project);

    /// <summary>
    /// Deletes a project by ID.
    /// </summary>
    /// <param name="projectId">The ID of the project to delete.</param>
    /// <returns>A task representing the operation.</returns>
    Task DeleteProjectAsync(string projectId);

    /// <summary>
    /// Sets the current active project.
    /// </summary>
    /// <param name="projectId">The ID of the project to set as current.</param>
    /// <returns>A task representing the operation.</returns>
    Task SetCurrentProjectAsync(string projectId);
}

using Standup.Domain.Entities;

namespace Standup.Domain.Interfaces;

/// <summary>
/// Service for interacting with the CRM system (Dynamics 365) to fetch project information.
/// </summary>
public interface ICrmProjectService
{
    /// <summary>
    /// Gets all CRM projects.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all CRM projects.</returns>
    Task<List<CrmProject>> GetAllProjectsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a CRM project by its ID.
    /// </summary>
    /// <param name="projectId">The CRM project ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The CRM project if found, otherwise null.</returns>
    Task<CrmProject?> GetProjectByIdAsync(string projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a CRM project by client code.
    /// </summary>
    /// <param name="clientCode">The client code to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The CRM project if found, otherwise null.</returns>
    Task<CrmProject?> GetProjectByClientCodeAsync(string clientCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets upcoming milestones for a project.
    /// </summary>
    /// <param name="projectId">The CRM project ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of upcoming milestones.</returns>
    Task<List<CrmMilestone>> GetUpcomingMilestonesAsync(string projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets in-progress tasks for a project (tasks that are not 100% complete).
    /// </summary>
    /// <param name="projectId">The CRM project ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of in-progress tasks.</returns>
    Task<List<CrmTask>> GetInProgressTasksAsync(string projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the CRM connection and credentials.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if connection is valid, otherwise false.</returns>
    Task<bool> ValidateConnectionAsync(CancellationToken cancellationToken = default);
}

using Standup.Domain.Entities;

namespace Standup.Application.Interfaces;

/// <summary>
/// Repository interface for repository groups persistence.
/// </summary>
public interface IGroupRepository
{
    /// <summary>
    /// Gets all repository groups.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all groups.</returns>
    Task<IEnumerable<RepositoryGroup>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a repository group by ID.
    /// </summary>
    /// <param name="groupId">The group ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The group, or null if not found.</returns>
    Task<RepositoryGroup?> GetByIdAsync(string groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default repository group.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The default group, or null if none is set.</returns>
    Task<RepositoryGroup?> GetDefaultAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new repository group.
    /// </summary>
    /// <param name="group">The group to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The added group.</returns>
    Task<RepositoryGroup> AddAsync(RepositoryGroup group, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing repository group.
    /// </summary>
    /// <param name="group">The group with updated values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated group.</returns>
    Task<RepositoryGroup> UpdateAsync(RepositoryGroup group, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a repository group by ID.
    /// </summary>
    /// <param name="groupId">The ID of the group to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task DeleteAsync(string groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a group as the default.
    /// </summary>
    /// <param name="groupId">The ID of the group to set as default.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task SetDefaultAsync(string groupId, CancellationToken cancellationToken = default);
}

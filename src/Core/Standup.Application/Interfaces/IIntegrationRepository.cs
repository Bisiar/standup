using Standup.Domain.Entities;
using Standup.Domain.Enums;

namespace Standup.Application.Interfaces;

/// <summary>
/// Repository for managing integration settings.
/// </summary>
public interface IIntegrationRepository
{
    /// <summary>
    /// Gets all integration settings.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of all integration settings.</returns>
    Task<IEnumerable<IntegrationSettings>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets integration settings by type.
    /// </summary>
    /// <param name="type">The type of integration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The integration settings, or null if not found.</returns>
    Task<IntegrationSettings?> GetByTypeAsync(IntegrationType type, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds new integration settings.
    /// </summary>
    /// <param name="settings">The integration settings to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The added integration settings.</returns>
    Task<IntegrationSettings> AddAsync(IntegrationSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates existing integration settings.
    /// </summary>
    /// <param name="settings">The integration settings to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated integration settings.</returns>
    Task<IntegrationSettings> UpdateAsync(IntegrationSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes integration settings by ID.
    /// </summary>
    /// <param name="id">The ID of the integration settings to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}

using Standup.Domain.Entities;
using Standup.Domain.Enums;

namespace Standup.Application.Interfaces;

/// <summary>
/// Repository interface for organization credentials persistence.
/// </summary>
public interface ICredentialRepository
{
    /// <summary>
    /// Gets all stored credentials.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all credentials.</returns>
    Task<IEnumerable<OrgCredential>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a credential by organization.
    /// </summary>
    /// <param name="sourceType">The type of source control system.</param>
    /// <param name="organization">The organization name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The credential, or null if not found.</returns>
    Task<OrgCredential?> GetByOrgAsync(SourceType sourceType, string organization, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a credential (creates or updates).
    /// </summary>
    /// <param name="credential">The credential to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saved credential.</returns>
    Task<OrgCredential> SaveAsync(OrgCredential credential, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a credential by ID.
    /// </summary>
    /// <param name="credentialId">The credential ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task DeleteAsync(string credentialId, CancellationToken cancellationToken = default);
}

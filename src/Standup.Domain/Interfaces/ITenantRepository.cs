using Standup.Domain.Entities;

namespace Standup.Domain.Interfaces;

/// <summary>
/// Repository for tenant-specific operations.
/// </summary>
public interface ITenantRepository : IRepository<Tenant>
{
    /// <summary>
    /// Gets a tenant by its Entra (Azure AD) tenant ID.
    /// </summary>
    /// <param name="entraTenantId">The Entra tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The tenant, or null if not found.</returns>
    Task<Tenant?> GetByEntraTenantIdAsync(string entraTenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a tenant by name.
    /// </summary>
    /// <param name="name">The tenant name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The tenant, or null if not found.</returns>
    Task<Tenant?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
}

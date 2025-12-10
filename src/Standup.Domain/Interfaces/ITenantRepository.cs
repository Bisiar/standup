using Standup.Domain.Entities;

namespace Standup.Domain.Interfaces;

/// <summary>
/// Repository for tenant-specific operations
/// </summary>
public interface ITenantRepository : IRepository<Tenant>
{
    Task<Tenant?> GetByEntraTenantIdAsync(string entraTenantId, CancellationToken cancellationToken = default);
    Task<Tenant?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
}

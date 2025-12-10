using Standup.Domain.Entities;

namespace Standup.Domain.Interfaces;

/// <summary>
/// Repository for user-specific operations
/// </summary>
public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEntraUserIdAsync(string entraUserId, string tenantId, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, string tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetByTenantAsync(string tenantId, CancellationToken cancellationToken = default);
    Task<User?> GetWithRepositoriesAsync(string userId, CancellationToken cancellationToken = default);
}

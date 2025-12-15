using Standup.Domain.Entities;

namespace Standup.Domain.Interfaces;

/// <summary>
/// Repository for user-specific operations.
/// </summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>
    /// Gets a user by their Entra (Azure AD) user ID.
    /// </summary>
    /// <param name="entraUserId">The Entra user ID.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user, or null if not found.</returns>
    Task<User?> GetByEntraUserIdAsync(string entraUserId, string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by email address.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user, or null if not found.</returns>
    Task<User?> GetByEmailAsync(string email, string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all users for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of users in the tenant.</returns>
    Task<IEnumerable<User>> GetByTenantAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user with their repositories loaded.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user with repositories, or null if not found.</returns>
    Task<User?> GetWithRepositoriesAsync(string userId, CancellationToken cancellationToken = default);
}

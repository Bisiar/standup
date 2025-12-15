namespace Standup.Application.Interfaces;

/// <summary>
/// Provides tenant and user context for multi-tenant operations.
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// Gets the current tenant ID.
    /// </summary>
    string? TenantId { get; }

    /// <summary>
    /// Gets the current user ID.
    /// </summary>
    string? UserId { get; }

    /// <summary>
    /// Sets the current tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID to set.</param>
    void SetTenant(string tenantId);

    /// <summary>
    /// Sets the current user.
    /// </summary>
    /// <param name="userId">The user ID to set.</param>
    void SetUser(string userId);
}

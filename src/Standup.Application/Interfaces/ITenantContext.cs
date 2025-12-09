namespace Standup.Application.Interfaces;

public interface ITenantContext
{
    string? TenantId { get; }
    string? UserId { get; }
    void SetTenant(string tenantId);
    void SetUser(string userId);
}

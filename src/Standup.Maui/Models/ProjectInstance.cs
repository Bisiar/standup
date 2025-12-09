namespace Standup.Maui.Models;

public class ProjectInstance
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public string ApiEndpoint { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? TenantId { get; set; }
    public string? AccessToken { get; set; }
    public bool IsDefault { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

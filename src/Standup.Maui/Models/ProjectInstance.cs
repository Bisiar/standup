using Standup.Domain.Enums;

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

    // Local source configuration
    public SourceType SourceType { get; set; } = SourceType.AzureDevOps;
    public string? SourceOrganization { get; set; }
    public string? SourceProject { get; set; }
    public string? SourceRepository { get; set; }
    public string? SourcePat { get; set; }
    public string? AuthorIdentifier { get; set; }
    public bool UseLocalGeneration { get; set; } = true;
}

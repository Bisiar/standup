using Standup.Domain.Enums;

namespace Standup.Application.Models;

public record ProjectInstance(
    string Id,
    string Name,
    string TenantName,
    string ApiEndpoint,
    string? UserId = null,
    string? TenantId = null,
    string? AccessToken = null,
    bool IsDefault = false,
    SourceType SourceType = SourceType.AzureDevOps,
    string? SourceOrganization = null,
    string? SourceProject = null,
    string? SourceRepository = null,
    string? SourcePat = null,
    string? AuthorIdentifier = null,
    bool UseLocalGeneration = true)
{
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

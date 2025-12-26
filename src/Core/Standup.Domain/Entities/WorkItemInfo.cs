using Standup.Domain.Enums;

namespace Standup.Domain.Entities;

public record WorkItemInfo(
    string Id,
    string Title,
    string Type,
    WorkItemStatus Status,
    SourceType SourceType,
    string Url,
    string? AssignedTo = null,
    string? ParentId = null,
    List<string>? Tags = null)
{
    public List<string> Tags { get; init; } = Tags ?? new();
}

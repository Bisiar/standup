using Standup.Domain.Enums;

namespace Standup.Domain.Entities;

public record PullRequestInfo(
    string Id,
    string Title,
    string Repository,
    SourceType SourceType,
    string Status,
    string Url,
    DateTimeOffset CreatedAt,
    string? Description = null,
    bool IsDraft = false,
    int ReviewerCount = 0);

using Standup.Domain.Enums;

namespace Standup.Domain.Entities;

public record CommitInfo(
    string Sha,
    string Message,
    string Repository,
    SourceType SourceType,
    DateTimeOffset CommittedAt,
    List<string>? FilesChanged = null,
    int Additions = 0,
    int Deletions = 0)
{
    public List<string> FilesChanged { get; init; } = FilesChanged ?? new();
}

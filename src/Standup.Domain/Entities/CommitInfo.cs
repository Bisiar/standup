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
    int Deletions = 0,
    string? Branch = null,
    string? Author = null,
    string? AuthorEmail = null)
{
    public List<string> FilesChanged { get; init; } = FilesChanged ?? new();

    /// <summary>
    /// Gets a short SHA (first 7 characters) for display.
    /// </summary>
    public string ShortSha => Sha.Length >= 7 ? Sha[..7] : Sha;

    /// <summary>
    /// Gets the first line of the commit message (subject).
    /// </summary>
    public string Subject => Message.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? Message;
}

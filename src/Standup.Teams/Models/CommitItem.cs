namespace Standup.Teams.Models;

public sealed record CommitItem(
    string Id,
    string Message,
    string Repository,
    DateTimeOffset Timestamp);

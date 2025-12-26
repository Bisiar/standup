namespace Standup.Teams.Models;

public sealed record PullRequestItem(
    int Id,
    string Title,
    string Status,
    string Repository,
    string Url);

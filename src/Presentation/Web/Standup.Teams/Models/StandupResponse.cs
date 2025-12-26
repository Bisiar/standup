namespace Standup.Teams.Models;

public sealed record StandupResponse(
    DateOnly Date,
    string Summary,
    IReadOnlyList<CommitItem> Commits,
    IReadOnlyList<PullRequestItem> PullRequests,
    IReadOnlyList<WorkItemItem> WorkItems);

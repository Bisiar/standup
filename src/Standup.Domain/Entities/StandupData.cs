namespace Standup.Domain.Entities;

public record StandupData(
    List<CommitInfo>? Commits = null,
    List<PullRequestInfo>? PullRequests = null,
    List<WorkItemInfo>? WorkItems = null)
{
    public List<CommitInfo> Commits { get; init; } = Commits ?? new();
    public List<PullRequestInfo> PullRequests { get; init; } = PullRequests ?? new();
    public List<WorkItemInfo> WorkItems { get; init; } = WorkItems ?? new();
}

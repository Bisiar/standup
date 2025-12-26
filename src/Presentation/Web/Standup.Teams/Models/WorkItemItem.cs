namespace Standup.Teams.Models;

public sealed record WorkItemItem(
    int Id,
    string Title,
    string Type,
    string Status);

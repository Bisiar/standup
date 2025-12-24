namespace Standup.Domain.Entities;

/// <summary>
/// Represents a task suggested by AI based on client email content.
/// </summary>
public record SuggestedTask(
    string Title,
    string Description,
    string Priority,
    string SourceEmailId)
{
    /// <summary>
    /// Gets the priority level (High, Medium, Low).
    /// </summary>
    public string Priority { get; init; } = Priority;

    /// <summary>
    /// Gets a value indicating whether this task has high priority.
    /// </summary>
    public bool IsHighPriority => Priority.Equals("High", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a truncated description for display.
    /// </summary>
    public string TruncatedDescription => Description.Length > 100 ? Description[..97] + "..." : Description;
}

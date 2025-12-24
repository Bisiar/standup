namespace Standup.Domain.Entities;

/// <summary>
/// Represents an email from a client that will be included in standup reports.
/// </summary>
public record ClientEmail(
    string EmailId,
    string ClientCode,
    string Subject,
    string Preview,
    string SenderName,
    string SenderEmail,
    DateTimeOffset ReceivedAt,
    bool IsImportant = false,
    bool HasAttachments = false,
    string? ConversationId = null,
    string? AiSummary = null,
    List<SuggestedTask>? SuggestedTasks = null)
{
    /// <summary>
    /// Gets the list of suggested tasks generated from this email.
    /// </summary>
    public List<SuggestedTask> SuggestedTasks { get; init; } = SuggestedTasks ?? new();

    /// <summary>
    /// Gets a truncated subject line for display.
    /// </summary>
    public string TruncatedSubject => Subject.Length > 60 ? Subject[..57] + "..." : Subject;

    /// <summary>
    /// Gets a truncated preview for display.
    /// </summary>
    public string TruncatedPreview => Preview.Length > 150 ? Preview[..147] + "..." : Preview;
}

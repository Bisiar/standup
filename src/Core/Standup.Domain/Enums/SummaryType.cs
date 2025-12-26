namespace Standup.Domain.Enums;

/// <summary>
/// Type of AI-generated summary for standup reports.
/// </summary>
public enum SummaryType
{
    /// <summary>
    /// Developer-focused: code changes, APIs, architecture decisions.
    /// </summary>
    Technical,

    /// <summary>
    /// High-level: features delivered, progress toward goals, blockers.
    /// </summary>
    Executive,

    /// <summary>
    /// Security-focused: vulnerabilities, code quality, risks.
    /// </summary>
    CodeReview
}

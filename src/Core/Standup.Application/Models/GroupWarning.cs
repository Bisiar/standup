namespace Standup.Application.Models;

/// <summary>
/// Severity level for group health warnings.
/// </summary>
public enum WarningSeverity
{
    /// <summary>
    /// Critical issues that prevent data fetching (e.g., fetch errors).
    /// </summary>
    Critical,

    /// <summary>
    /// Configuration warnings that may limit functionality (e.g., missing PAT).
    /// </summary>
    Warning,

    /// <summary>
    /// Informational messages (e.g., no activity in period).
    /// </summary>
    Info,
}

/// <summary>
/// Represents a health warning for a group or repository.
/// </summary>
public class GroupWarning
{
    /// <summary>
    /// Gets the severity level of the warning.
    /// </summary>
    public WarningSeverity Severity { get; init; }

    /// <summary>
    /// Gets the client code associated with this warning.
    /// </summary>
    public string ClientCode { get; init; } = string.Empty;

    /// <summary>
    /// Gets the repository name if this warning is repo-specific.
    /// </summary>
    public string? RepositoryName { get; init; }

    /// <summary>
    /// Gets the warning message.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Gets additional details about the warning.
    /// </summary>
    public string? Details { get; init; }

    /// <summary>
    /// Gets the target tab to navigate to for fixing this issue.
    /// </summary>
    public string? FixTarget { get; init; }

    /// <summary>
    /// Gets the icon for this warning based on severity.
    /// </summary>
    public string Icon => Severity switch
    {
        WarningSeverity.Critical => "❌",
        WarningSeverity.Warning => "🔒",
        WarningSeverity.Info => "💤",
        _ => "ℹ️",
    };

    /// <summary>
    /// Gets the action text for the fix button.
    /// </summary>
    public string? ActionText => FixTarget switch
    {
        "Settings" => "Fix →",
        "Groups" => "Configure →",
        "Projects" => "Configure →",
        _ => null,
    };

    /// <summary>
    /// Gets the border color for this warning based on severity.
    /// </summary>
    public string SeverityColor => Severity switch
    {
        WarningSeverity.Critical => "#EF4444",
        WarningSeverity.Warning => "#F59E0B",
        WarningSeverity.Info => "#3B82F6",
        _ => "#64748B",
    };
}

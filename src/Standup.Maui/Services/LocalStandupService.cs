using Standup.Application.DTOs;
using Standup.Domain.Enums;

namespace Standup.Maui.Services;

/// <summary>
/// Stub implementation for local standup generation.
/// On MacCatalyst, this returns a placeholder - use the API-based generation instead.
/// The full implementation requires Infrastructure which has AspNetCore dependencies
/// that don't work on MacCatalyst.
/// </summary>
public sealed class LocalStandupService : ILocalStandupService
{
    public Task<StandupReportDto> GenerateStandupAsync(
        SourceType sourceType,
        string organization,
        string project,
        string repository,
        string pat,
        string? authorIdentifier = null,
        CancellationToken cancellationToken = default)
    {
        // Return a placeholder report - local generation requires Infrastructure which has AspNetCore dependencies
        var report = new StandupReportDto(
            Id: Guid.NewGuid().ToString(),
            Summary: "## Standup Report\n\nLocal generation is not available on this platform.\nPlease configure API endpoint in Settings to generate standups.",
            PeriodStart: DateTimeOffset.UtcNow.AddDays(-1),
            PeriodEnd: DateTimeOffset.UtcNow,
            GeneratedAt: DateTimeOffset.UtcNow,
            CommitCount: 0,
            PullRequestCount: 0,
            WorkItemCount: 0,
            SentTo: new List<NotificationChannel>());

        return Task.FromResult(report);
    }

    public Task<bool> ValidateConnectionAsync(
        SourceType sourceType,
        string organization,
        string project,
        string repository,
        string pat,
        CancellationToken cancellationToken = default)
    {
        // Return false - local validation not available on this platform
        return Task.FromResult(false);
    }
}

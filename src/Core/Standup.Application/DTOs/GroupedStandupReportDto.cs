using Standup.Domain.Enums;

namespace Standup.Application.DTOs;

/// <summary>
/// Standup report grouped by client codes within a repository group.
/// </summary>
public record GroupedStandupReportDto(
    string Id,
    string GroupName,
    List<ClientCodeSection> Sections,
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd,
    DateTimeOffset GeneratedAt,
    int TotalCommits,
    int TotalPullRequests,
    int TotalWorkItems,
    SummaryType CurrentSummaryType = SummaryType.Technical);

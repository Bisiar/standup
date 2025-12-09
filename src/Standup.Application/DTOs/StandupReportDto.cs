using Standup.Domain.Enums;

namespace Standup.Application.DTOs;

public record StandupReportDto(
    string Id,
    string Summary,
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd,
    DateTimeOffset GeneratedAt,
    int CommitCount,
    int PullRequestCount,
    int WorkItemCount,
    List<NotificationChannel> SentTo);

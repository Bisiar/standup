using MediatR;
using Standup.Application.DTOs;
using Standup.Application.Services;
using Standup.Domain.Entities;
using Standup.Domain.Enums;
using Standup.Domain.Interfaces;
using Standup.Domain.ValueObjects;

namespace Standup.Application.Features.GenerateStandup;

public class GenerateStandupHandler : IRequestHandler<GenerateStandupCommand, StandupReportDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IStandupAggregatorService _aggregatorService;
    private readonly IAISummaryService _summaryService;
    private readonly IEnumerable<INotificationService> _notificationServices;
    private readonly IRepository<StandupReport> _reportRepository;

    public GenerateStandupHandler(
        IUserRepository userRepository,
        IStandupAggregatorService aggregatorService,
        IAISummaryService summaryService,
        IEnumerable<INotificationService> notificationServices,
        IRepository<StandupReport> reportRepository)
    {
        _userRepository = userRepository;
        _aggregatorService = aggregatorService;
        _summaryService = summaryService;
        _notificationServices = notificationServices;
        _reportRepository = reportRepository;
    }

    public async Task<StandupReportDto> Handle(GenerateStandupCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetWithRepositoriesAsync(request.UserId, cancellationToken)
            ?? throw new InvalidOperationException($"User {request.UserId} not found");

        var dateRange = GetDateRange(request, user.Preferences);

        var standupData = await _aggregatorService.AggregateDataAsync(
            user.SourceRepositories.Where(r => r.IsActive).ToList(),
            dateRange.Start,
            dateRange.End,
            cancellationToken);

        var summary = await _summaryService.GenerateSummaryAsync(
            standupData,
            new SummaryOptions(Tone: SummaryTone.Professional),
            cancellationToken);

        var report = new StandupReport
        {
            UserId = user.Id,
            TenantId = user.TenantId,
            Summary = summary,
            RawData = standupData,
            PeriodStart = dateRange.Start,
            PeriodEnd = dateRange.End
        };

        var channels = request.SendTo ?? user.Preferences.NotificationChannels;
        await SendNotificationsAsync(user, report, channels, cancellationToken);

        if (request.SaveReport)
        {
            await _reportRepository.AddAsync(report, cancellationToken);
        }

        user.LastStandupAt = DateTimeOffset.UtcNow;
        await _userRepository.UpdateAsync(user, cancellationToken);

        return new StandupReportDto(
            report.Id,
            report.Summary,
            report.PeriodStart,
            report.PeriodEnd,
            report.GeneratedAt,
            standupData.Commits.Count,
            standupData.PullRequests.Count,
            standupData.WorkItems.Count,
            report.SentTo);
    }

    private static DateRange GetDateRange(GenerateStandupCommand request, UserPreferences preferences)
    {
        if (request.Since.HasValue && request.Until.HasValue)
        {
            return new DateRange(request.Since.Value, request.Until.Value);
        }

        return DateRange.LastWorkingDay(preferences.TimeZone, preferences.SkipWeekends);
    }

    private async Task SendNotificationsAsync(
        User user,
        StandupReport report,
        List<NotificationChannel> channels,
        CancellationToken cancellationToken)
    {
        foreach (var channel in channels)
        {
            var service = _notificationServices.FirstOrDefault(s => s.Channel == channel);
            if (service != null)
            {
                var sent = await service.SendStandupReportAsync(user, report, cancellationToken);
                if (sent)
                {
                    report.SentTo.Add(channel);
                }
            }
        }
    }
}

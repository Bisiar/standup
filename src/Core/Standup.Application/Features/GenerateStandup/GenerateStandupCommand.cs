using MediatR;
using Standup.Application.DTOs;
using Standup.Domain.Enums;

namespace Standup.Application.Features.GenerateStandup;

public record GenerateStandupCommand(
    string UserId,
    string TenantId,
    DateTimeOffset? Since = null,
    DateTimeOffset? Until = null,
    List<NotificationChannel>? SendTo = null,
    bool SaveReport = true) : IRequest<StandupReportDto>;

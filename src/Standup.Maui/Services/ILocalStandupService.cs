using Standup.Application.DTOs;
using Standup.Domain.Enums;

namespace Standup.Maui.Services;

public interface ILocalStandupService
{
    Task<StandupReportDto> GenerateStandupAsync(
        SourceType sourceType,
        string organization,
        string project,
        string repository,
        string pat,
        string? authorIdentifier = null,
        CancellationToken cancellationToken = default);

    Task<bool> ValidateConnectionAsync(
        SourceType sourceType,
        string organization,
        string project,
        string repository,
        string pat,
        CancellationToken cancellationToken = default);
}

using Standup.Application.DTOs;

namespace Standup.Maui.Services;

public interface IStandupApiClient
{
    void SetProject(string apiEndpoint, string? accessToken);
    Task<StandupReportDto> GenerateStandupAsync(string userId, string tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<SourceRepositoryDto>> GetRepositoriesAsync(string userId, CancellationToken cancellationToken = default);
    Task<SourceRepositoryDto> AddRepositoryAsync(string userId, CreateSourceRepositoryDto repository, CancellationToken cancellationToken = default);
}

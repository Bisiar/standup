using System.Net.Http.Headers;
using System.Net.Http.Json;
using Standup.Application.DTOs;
using Standup.Application.Features.ConfigureRepository;
using Standup.Application.Features.GenerateStandup;
using Standup.Application.Interfaces;

namespace Standup.Infrastructure.Http;

public class StandupApiClient : IStandupApiClient
{
    private readonly HttpClient _httpClient;

    public StandupApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public void SetProject(string apiEndpoint, string? accessToken)
    {
        _httpClient.BaseAddress = new Uri(apiEndpoint);

        if (!string.IsNullOrEmpty(accessToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);
        }
    }

    public async Task<StandupReportDto> GenerateStandupAsync(
        string userId,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        var command = new GenerateStandupCommand(userId, tenantId);
        var response = await HttpClientJsonExtensions.PostAsJsonAsync(_httpClient, "/api/standup/generate", command, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<StandupReportDto>(cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize response");
    }

    public async Task<IEnumerable<SourceRepositoryDto>> GetRepositoriesAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/api/standup/repositories?userId={userId}", cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<IEnumerable<SourceRepositoryDto>>(cancellationToken)
            ?? Enumerable.Empty<SourceRepositoryDto>();
    }

    public async Task<SourceRepositoryDto> AddRepositoryAsync(
        string userId,
        CreateSourceRepositoryDto repository,
        CancellationToken cancellationToken = default)
    {
        var command = new AddRepositoryCommand(userId, repository);
        var response = await HttpClientJsonExtensions.PostAsJsonAsync(_httpClient, "/api/standup/repositories", command, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<SourceRepositoryDto>(cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize response");
    }
}

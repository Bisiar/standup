using System.Net.Http.Json;
using Standup.Teams.Models;

namespace Standup.Teams.Services;

public sealed class StandupApiService : IStandupApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<StandupApiService> _logger;

    public StandupApiService(HttpClient httpClient, ILogger<StandupApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<StandupResponse> GetStandupAsync(string userId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/api/standup/{userId}", cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<StandupResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize standup response");
    }

    public async Task SubscribeAsync(string userId, string conversationId, CancellationToken cancellationToken = default)
    {
        var request = new SubscriptionRequest(userId, conversationId, "teams");
        var response = await HttpClientJsonExtensions.PostAsJsonAsync(_httpClient, "/api/subscriptions", request, cancellationToken: cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task UnsubscribeAsync(string userId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"/api/subscriptions/{userId}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task ShareStandupToChannelAsync(string userId, string channelId, CancellationToken cancellationToken = default)
    {
        var request = new ShareRequest(userId, channelId);
        var response = await HttpClientJsonExtensions.PostAsJsonAsync(_httpClient, "/api/standup/share", request, cancellationToken: cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}

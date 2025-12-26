using Standup.Teams.Models;

namespace Standup.Teams.Services;

public interface IStandupApiService
{
    Task<StandupResponse> GetStandupAsync(string userId, CancellationToken cancellationToken = default);
    Task SubscribeAsync(string userId, string conversationId, CancellationToken cancellationToken = default);
    Task UnsubscribeAsync(string userId, CancellationToken cancellationToken = default);
    Task ShareStandupToChannelAsync(string userId, string channelId, CancellationToken cancellationToken = default);
}

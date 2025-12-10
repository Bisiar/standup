namespace Standup.Teams.Models;

public sealed record SubscriptionRequest(
    string UserId,
    string ConversationId,
    string Channel);

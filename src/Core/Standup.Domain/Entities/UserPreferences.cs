using Standup.Domain.Enums;

namespace Standup.Domain.Entities;

public record UserPreferences(
    List<NotificationChannel>? NotificationChannels = null,
    string TimeZone = "America/Denver",
    bool IncludeCommitDetails = true,
    bool IncludePullRequests = true,
    bool IncludeWorkItems = true,
    int LookbackHours = 24,
    bool SkipWeekends = true)
{
    public List<NotificationChannel> NotificationChannels { get; init; } =
        NotificationChannels ?? new List<NotificationChannel> { NotificationChannel.TeamsDirectMessage };
}

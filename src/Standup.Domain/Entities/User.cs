namespace Standup.Domain.Entities;

public class User
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string TenantId { get; set; } = string.Empty;
    public string EntraUserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? TeamsUserId { get; set; }

    public UserPreferences Preferences { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastStandupAt { get; set; }

    public Tenant? Tenant { get; set; }
    public ICollection<SourceRepository> SourceRepositories { get; set; } = new List<SourceRepository>();
    public ICollection<StandupReport> StandupReports { get; set; } = new List<StandupReport>();
    public ICollection<TeamSubscription> Subscriptions { get; set; } = new List<TeamSubscription>();
}

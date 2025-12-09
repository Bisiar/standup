namespace Standup.Domain.Entities;

public class Tenant
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string EntraTenantId { get; set; } = string.Empty;
    public string? TeamsTeamId { get; set; }
    public string? DefaultChannelId { get; set; }
    public EmailSettings EmailSettings { get; set; } = new();
    public AISettings AISettings { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<StandupSchedule> Schedules { get; set; } = new List<StandupSchedule>();
}

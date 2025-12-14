namespace Standup.Domain.Entities;

/// <summary>
/// A group of repositories, typically representing a client or project collection.
/// </summary>
public class RepositoryGroup
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<GroupedRepository> Repositories { get; set; } = new();
}

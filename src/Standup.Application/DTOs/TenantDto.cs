namespace Standup.Application.DTOs;

public record TenantDto(
    string Id,
    string Name,
    bool IsActive,
    DateTimeOffset CreatedAt);

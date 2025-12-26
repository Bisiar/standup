using Standup.Domain.Entities;

namespace Standup.Application.DTOs;

public record UserDto(
    string Id,
    string DisplayName,
    string Email,
    string TenantId,
    UserPreferences Preferences,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastStandupAt);

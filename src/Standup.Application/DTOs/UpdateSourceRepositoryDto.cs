namespace Standup.Application.DTOs;

public record UpdateSourceRepositoryDto(
    string? DisplayName,
    string? AuthorIdentifier,
    string? PersonalAccessToken,
    string? DefaultBranch,
    bool? IsActive);

using Standup.Domain.Enums;

namespace Standup.Application.DTOs;

public record SourceRepositoryDto(
    string Id,
    SourceType SourceType,
    string Organization,
    string? Project,
    string Repository,
    string? DisplayName,
    string AuthorIdentifier,
    string DefaultBranch,
    bool IsActive);

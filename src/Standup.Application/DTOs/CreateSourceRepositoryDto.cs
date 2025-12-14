using Standup.Domain.Enums;

namespace Standup.Application.DTOs;

public record CreateSourceRepositoryDto(
    SourceType SourceType,
    string Organization,
    string? Project,
    string Repository,
    string? DisplayName,
    string AuthorIdentifier,
    string? PersonalAccessToken,
    string DefaultBranch = "main");

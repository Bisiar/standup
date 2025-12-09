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

public record CreateSourceRepositoryDto(
    SourceType SourceType,
    string Organization,
    string? Project,
    string Repository,
    string? DisplayName,
    string AuthorIdentifier,
    string? PersonalAccessToken,
    string DefaultBranch = "main");

public record UpdateSourceRepositoryDto(
    string? DisplayName,
    string? AuthorIdentifier,
    string? PersonalAccessToken,
    string? DefaultBranch,
    bool? IsActive);

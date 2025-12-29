using System.ComponentModel;
using System.Text.Json;
using MediatR;
using ModelContextProtocol.Server;
using Standup.Application.DTOs;
using Standup.Application.Features.ConfigureRepository;
using Standup.Domain.Enums;

namespace Standup.Api.Tools;

[McpServerToolType]
public sealed class ConfigurationTools
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly IMediator _mediator;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ConfigurationTools(IMediator mediator, IHttpContextAccessor httpContextAccessor)
    {
        _mediator = mediator;
        _httpContextAccessor = httpContextAccessor;
    }

    [McpServerTool]
    [Description("List all configured repositories for the current user")]
    public async Task<string> ListRepositories()
    {
        var userId = GetCurrentUserId();

        if (string.IsNullOrEmpty(userId))
        {
            return JsonSerializer.Serialize(new { error = "User not authenticated" });
        }

        var query = new ListRepositoriesQuery(userId);
        var result = await _mediator.Send(query);

        return JsonSerializer.Serialize(result, JsonOptions);
    }

    [McpServerTool]
    [Description("Add a new GitHub repository to track for standup reports")]
    public async Task<string> AddGitHubRepository(
        [Description("GitHub organization or username")] string organization,
        [Description("Repository name")] string repository,
        [Description("Your GitHub username for filtering commits")] string authorUsername,
        [Description("Optional: Display name for this repository")] string? displayName = null,
        [Description("Optional: Default branch (defaults to 'main')")] string defaultBranch = "main",
        [Description("Optional: Personal Access Token for private repos")] string? pat = null)
    {
        var userId = GetCurrentUserId();

        if (string.IsNullOrEmpty(userId))
        {
            return JsonSerializer.Serialize(new { error = "User not authenticated" });
        }

        var dto = new CreateSourceRepositoryDto(
            SourceType: SourceType.GitHub,
            Organization: organization,
            Project: null,
            Repository: repository,
            DisplayName: displayName,
            AuthorIdentifier: authorUsername,
            PersonalAccessToken: pat,
            DefaultBranch: defaultBranch);

        var command = new AddRepositoryCommand(userId, dto);
        var result = await _mediator.Send(command);

        return JsonSerializer.Serialize(result, JsonOptions);
    }

    [McpServerTool]
    [Description("Add a new Azure DevOps repository to track for standup reports")]
    public async Task<string> AddAzureDevOpsRepository(
        [Description("Azure DevOps organization name")] string organization,
        [Description("Project name")] string project,
        [Description("Repository name")] string repository,
        [Description("Your email or display name for filtering commits/work items")] string authorIdentifier,
        [Description("Optional: Display name for this repository")] string? displayName = null,
        [Description("Optional: Default branch (defaults to 'main')")] string defaultBranch = "main",
        [Description("Optional: Personal Access Token")] string? pat = null)
    {
        var userId = GetCurrentUserId();

        if (string.IsNullOrEmpty(userId))
        {
            return JsonSerializer.Serialize(new { error = "User not authenticated" });
        }

        var dto = new CreateSourceRepositoryDto(
            SourceType: SourceType.AzureDevOps,
            Organization: organization,
            Project: project,
            Repository: repository,
            DisplayName: displayName,
            AuthorIdentifier: authorIdentifier,
            PersonalAccessToken: pat,
            DefaultBranch: defaultBranch);

        var command = new AddRepositoryCommand(userId, dto);
        var result = await _mediator.Send(command);

        return JsonSerializer.Serialize(result, JsonOptions);
    }

    private string? GetCurrentUserId()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirst("oid")?.Value
            ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;
    }
}

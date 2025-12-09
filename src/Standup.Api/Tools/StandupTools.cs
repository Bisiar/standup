using MediatR;
using Microsoft.AspNetCore.Http;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using Standup.Application.DTOs;
using Standup.Application.Features.GenerateStandup;
using Standup.Domain.Enums;
using System.ComponentModel;
using System.Text.Json;

namespace Standup.Api.Tools;

[McpServerToolType]
public sealed class StandupTools
{
    private readonly IMediator _mediator;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public StandupTools(IMediator mediator, IHttpContextAccessor httpContextAccessor)
    {
        _mediator = mediator;
        _httpContextAccessor = httpContextAccessor;
    }

    [McpServerTool, Description("Generate a standup report for the current user based on their configured repositories")]
    public async Task<string> GenerateStandup(
        [Description("Optional: Custom date range start (ISO 8601 format)")] string? since = null,
        [Description("Optional: Custom date range end (ISO 8601 format)")] string? until = null,
        [Description("Optional: Send to specific channels (TeamsDirectMessage, TeamsChannel, Email)")] string? sendTo = null)
    {
        var userId = GetCurrentUserId();
        var tenantId = GetCurrentTenantId();

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tenantId))
        {
            return JsonSerializer.Serialize(new { error = "User not authenticated or tenant not configured" });
        }

        var channels = ParseChannels(sendTo);

        var command = new GenerateStandupCommand(
            UserId: userId,
            TenantId: tenantId,
            Since: ParseDate(since),
            Until: ParseDate(until),
            SendTo: channels);

        var result = await _mediator.Send(command);

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool, Description("Get the current user's standup summary without sending notifications")]
    public async Task<string> PreviewStandup(
        [Description("Optional: Custom date range start (ISO 8601 format)")] string? since = null,
        [Description("Optional: Custom date range end (ISO 8601 format)")] string? until = null)
    {
        var userId = GetCurrentUserId();
        var tenantId = GetCurrentTenantId();

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tenantId))
        {
            return JsonSerializer.Serialize(new { error = "User not authenticated or tenant not configured" });
        }

        var command = new GenerateStandupCommand(
            UserId: userId,
            TenantId: tenantId,
            Since: ParseDate(since),
            Until: ParseDate(until),
            SendTo: new List<NotificationChannel>(),
            SaveReport: false);

        var result = await _mediator.Send(command);

        return result.Summary;
    }

    private string? GetCurrentUserId()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirst("oid")?.Value
            ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;
    }

    private string? GetCurrentTenantId()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirst("tid")?.Value;
    }

    private static DateTimeOffset? ParseDate(string? dateStr)
    {
        if (string.IsNullOrEmpty(dateStr))
            return null;

        return DateTimeOffset.TryParse(dateStr, out var result) ? result : null;
    }

    private static List<NotificationChannel>? ParseChannels(string? channelsStr)
    {
        if (string.IsNullOrEmpty(channelsStr))
            return null;

        return channelsStr.Split(',')
            .Select(c => Enum.TryParse<NotificationChannel>(c.Trim(), true, out var channel) ? channel : (NotificationChannel?)null)
            .Where(c => c.HasValue)
            .Select(c => c!.Value)
            .ToList();
    }
}

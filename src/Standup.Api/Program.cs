using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.AspNetCore;
using Standup.Api.Tools;
using Standup.Application;
using Standup.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:8080");

// Add application and infrastructure services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Configure MCP Server
builder.Services.AddMcpServer()
    .WithHttpTransport(options =>
    {
        options.Stateless = true;
    })
    .WithTools<StandupTools>()
    .WithTools<ConfigurationTools>();

builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Error;
});

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Root endpoint
app.MapGet("/", () => "Standup Automation API is running.");

// Health check
app.MapGet("/api/healthz", () => "Healthy");

// MCP endpoints
app.MapMcp(pattern: "/mcp");

// REST API endpoints for MAUI app
app.MapGroup("/api/standup")
    .MapStandupEndpoints();

await app.RunAsync();

public static class StandupEndpoints
{
    public static RouteGroupBuilder MapStandupEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/generate", async (
            MediatR.IMediator mediator,
            Standup.Application.Features.GenerateStandup.GenerateStandupCommand command) =>
        {
            var result = await mediator.Send(command);
            return Results.Ok(result);
        });

        group.MapGet("/repositories", async (
            MediatR.IMediator mediator,
            string userId) =>
        {
            var query = new Standup.Application.Features.ConfigureRepository.ListRepositoriesQuery(userId);
            var result = await mediator.Send(query);
            return Results.Ok(result);
        });

        group.MapPost("/repositories", async (
            MediatR.IMediator mediator,
            Standup.Application.Features.ConfigureRepository.AddRepositoryCommand command) =>
        {
            var result = await mediator.Send(command);
            return Results.Created($"/api/standup/repositories/{result.Id}", result);
        });

        return group;
    }
}

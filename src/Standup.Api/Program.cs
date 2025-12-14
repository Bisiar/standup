using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.AspNetCore;
using Standup.Api;
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

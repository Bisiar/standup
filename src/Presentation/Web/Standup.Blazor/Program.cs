// <copyright file="Program.cs" company="Standup">
// Copyright (c) Standup. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Serilog;
using Standup.Application.Interfaces;
using Standup.Application.Services;
using Standup.Application.ViewModels;
using Standup.Blazor.Components;
using Standup.Blazor.Services;
using Standup.Domain.Interfaces;
using Standup.Infrastructure.Git;
using Standup.Infrastructure.Integrations;
using Standup.Infrastructure.Services;
using Standup.Infrastructure.SourceProviders;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/standup-blazor-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting Standup Blazor Server");

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog();

    // Add Data Protection for encryption
    builder.Services.AddDataProtection();

    // Only add Azure AD authentication if configured
    var azureTenantId = builder.Configuration["AzureAd:TenantId"];
    var isAuthConfigured = !string.IsNullOrEmpty(azureTenantId) && azureTenantId != "your-tenant-id";

    if (isAuthConfigured)
    {
        builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

        builder.Services.AddControllersWithViews()
            .AddMicrosoftIdentityUI();

        builder.Services.AddAuthorization(options =>
        {
            options.FallbackPolicy = options.DefaultPolicy;
        });
    }
    else
    {
        // Development mode - no auth required
        Log.Warning("Azure AD not configured - running without authentication");
        builder.Services.AddAuthorization();
    }

    // Add Razor components
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    // Add Telerik Blazor
    builder.Services.AddTelerikBlazor();

    // ===== Blazor-Specific Service Implementations =====
    builder.Services.AddSingleton<IEncryptionService, DataProtectionEncryptionService>();
    builder.Services.AddSingleton<IGroupRepository, JsonFileGroupRepository>();
    builder.Services.AddSingleton<ICredentialRepository, JsonFileCredentialRepository>();
    builder.Services.AddSingleton<IReportHistoryRepository, JsonFileReportHistoryRepository>();
    builder.Services.AddSingleton<IProjectService, JsonFileProjectService>();
    builder.Services.AddScoped<IClipboardService, BlazorClipboardService>();

    // ===== Infrastructure Services (Shared with MAUI) =====
    builder.Services.AddSingleton<LocalGitService>();
    builder.Services.AddSingleton<GitHubSourceProvider>();
    builder.Services.AddSingleton<AzureDevOpsSourceProvider>();
    builder.Services.AddSingleton<ISourceProviderFactory, SourceProviderFactory>();
    builder.Services.AddSingleton<ILocalStandupService, LocalStandupService>();
    builder.Services.AddHttpClient<ICrmProjectService, DynamicsCrmService>();

    // ===== Application Services =====
    builder.Services.AddSingleton<GroupService>();
    builder.Services.AddSingleton<CredentialService>();
    builder.Services.AddSingleton<ReportHistoryService>();

    // ===== ViewModels =====
    builder.Services.AddScoped<MainViewModel>();
    builder.Services.AddScoped<DashboardViewModel>();
    builder.Services.AddScoped<StandupViewModel>();
    builder.Services.AddScoped<ProjectListViewModel>();
    builder.Services.AddScoped<ProjectDashboardViewModel>();
    builder.Services.AddScoped<GroupListViewModel>();
    builder.Services.AddScoped<ReportViewModel>();
    builder.Services.AddScoped<SettingsViewModel>();
    builder.Services.AddScoped<FrameworkViewModel>();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        app.UseHsts();
    }

    app.UseStaticFiles();
    app.UseAntiforgery();

    if (isAuthConfigured)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
    }

    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

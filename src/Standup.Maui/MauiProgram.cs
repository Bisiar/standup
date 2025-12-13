// using CommunityToolkit.Maui; // Temporarily disabled to test crash
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Standup.Application.Interfaces;
using Standup.Application.Services;
using Standup.Maui.Repositories;
using Standup.Maui.Services;
using Standup.Maui.ViewModels;
using Standup.Maui.Views;
#if MACCATALYST
using Standup.Maui.Platforms.MacCatalyst.Helpers;
#endif

namespace Standup.Maui;

public static class MauiProgram
{
    public static IServiceProvider? ServiceProvider { get; private set; }
    private static readonly string AppVersion = AppInfo.VersionString;

    public static MauiApp CreateMauiApp()
    {
        try
        {
            ConfigureLogging();
            Log.Information("Standup.Maui starting - Version {Version}", AppVersion);

#if MACCATALYST
            DiagnosticsHelper.Initialize();
            DiagnosticsHelper.LogInfo($"App Version: {AppVersion}");
#endif
        }
        catch (Exception ex)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();
            Log.Error(ex, "Failed to initialize logging");
        }

        try
        {
            Log.Information("Creating MauiApp builder");
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                // .UseMauiCommunityToolkit() // Temporarily disabled to test if this causes crash
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Services
            Log.Debug("Registering services");
            builder.Services.AddSingleton<IProjectService, ProjectService>();
            builder.Services.AddSingleton<IStandupApiClient, StandupApiClient>();
            builder.Services.AddSingleton<IDocumentService, DocumentService>();
            builder.Services.AddSingleton<IEncryptionService, MauiEncryptionService>();
            builder.Services.AddSingleton<ILocalStandupService, LocalStandupService>();

            // Repository Groups - Repositories (MAUI-specific Preferences-based persistence)
            builder.Services.AddSingleton<IGroupRepository, PreferencesGroupRepository>();
            builder.Services.AddSingleton<ICredentialRepository, PreferencesCredentialRepository>();
            builder.Services.AddSingleton<IReportHistoryRepository, PreferencesReportHistoryRepository>();

            // Repository Groups - Application Services
            builder.Services.AddSingleton<GroupService>();
            builder.Services.AddSingleton<CredentialService>();

            // HTTP Client
            builder.Services.AddHttpClient<IStandupApiClient, StandupApiClient>(client =>
            {
                // Base URL will be set per project instance
            });

            // ViewModels
            Log.Debug("Registering ViewModels");
            builder.Services.AddTransient<MainViewModel>();
            builder.Services.AddTransient<ProjectListViewModel>();
            builder.Services.AddTransient<StandupViewModel>();
            builder.Services.AddTransient<SettingsViewModel>();
            builder.Services.AddTransient<RepositoryConfigViewModel>();
            builder.Services.AddTransient<DocumentsViewModel>();

            // Views
            Log.Debug("Registering Views");
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<ProjectListPage>();
            builder.Services.AddTransient<StandupPage>();
            builder.Services.AddTransient<SettingsPage>();
            builder.Services.AddTransient<RepositoryConfigPage>();
            builder.Services.AddTransient<DocumentsPage>();

            // Configure logging
            Log.Information("Configuring logging providers");
            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog();

            Log.Information("Building MauiApp");
            var app = builder.Build();

            ServiceProvider = app.Services;

            Log.Information("MauiApp created successfully");
            return app;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Failed to create MauiApp");
#if MACCATALYST
            DiagnosticsHelper.LogCritical($"FATAL: Failed to create MauiApp: {ex}");
#endif
            Console.WriteLine($"FATAL ERROR: {ex}");
            throw;
        }
    }

    private static void ConfigureLogging()
    {
        var logConfig = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .WriteTo.Console(outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
            .Enrich.FromLogContext();

#if DEBUG
        try
        {
            // Write to project directory logs folder for debugging
            var projectLogPath = "/Users/james/Source/github.com.bisiar/standup/src/Standup.Maui/logs";
            Directory.CreateDirectory(projectLogPath);
            var logFileName = Path.Combine(projectLogPath, "standup_maui_.log");

            logConfig.WriteTo.File(
                formatter: new Serilog.Formatting.Compact.CompactJsonFormatter(),
                path: logFileName,
                rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: true,
                fileSizeLimitBytes: 10_000_000, // 10MB
                retainedFileCountLimit: 10);

            Console.WriteLine($"Debug log file: {logFileName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to setup file logging: {ex.Message}");

            // Fallback to app data directory
            try
            {
                var logPath = Path.Combine(FileSystem.AppDataDirectory, "logs");
                Directory.CreateDirectory(logPath);
                var logFileName = Path.Combine(logPath, "standup_maui_.log");

                logConfig.WriteTo.File(
                    formatter: new Serilog.Formatting.Compact.CompactJsonFormatter(),
                    path: logFileName,
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true,
                    fileSizeLimitBytes: 10_000_000,
                    retainedFileCountLimit: 10);

                Console.WriteLine($"Fallback log file: {logFileName}");
            }
            catch (Exception fallbackEx)
            {
                Console.WriteLine($"Failed to setup fallback logging: {fallbackEx.Message}");
            }
        }
#endif

        Log.Logger = logConfig.CreateLogger();
        Log.Information("Serilog logging initialized");
    }
}

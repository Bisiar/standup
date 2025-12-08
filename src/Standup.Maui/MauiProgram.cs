using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Standup.Maui.Services;
using Standup.Maui.ViewModels;
using Standup.Maui.Views;

namespace Standup.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Services
        builder.Services.AddSingleton<IProjectService, ProjectService>();
        builder.Services.AddSingleton<IStandupApiClient, StandupApiClient>();
        builder.Services.AddSingleton<IDocumentService, DocumentService>();

        // HTTP Client
        builder.Services.AddHttpClient<IStandupApiClient, StandupApiClient>(client =>
        {
            // Base URL will be set per project instance
        });

        // ViewModels
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<ProjectListViewModel>();
        builder.Services.AddTransient<StandupViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<RepositoryConfigViewModel>();
        builder.Services.AddTransient<DocumentsViewModel>();

        // Views
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<ProjectListPage>();
        builder.Services.AddTransient<StandupPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<RepositoryConfigPage>();
        builder.Services.AddTransient<DocumentsPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}

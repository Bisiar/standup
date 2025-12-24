using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Standup.Application.Interfaces;
using Standup.Domain.Interfaces;
using Standup.Infrastructure.AI;
using Standup.Infrastructure.Configuration;
using Standup.Infrastructure.Integrations;
using Standup.Infrastructure.Notifications;
using Standup.Infrastructure.SourceProviders;

namespace Standup.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configuration options
        services.Configure<AIFoundryOptions>(configuration.GetSection(AIFoundryOptions.SectionName));
        services.Configure<EncryptionOptions>(configuration.GetSection(EncryptionOptions.SectionName));
        services.Configure<DynamicsCrmOptions>(configuration.GetSection(DynamicsCrmOptions.SectionName));

        // Encryption
        services.AddSingleton<IEncryptionService, EncryptionService>();

        // Source providers
        services.AddScoped<GitHubSourceProvider>();
        services.AddScoped<AzureDevOpsSourceProvider>();
        services.AddScoped<ISourceProviderFactory, SourceProviderFactory>();

        // AI services
        services.AddScoped<IAISummaryService, AIFoundrySummaryService>();

        // CRM integration
        services.AddScoped<ICrmProjectService>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<DynamicsCrmOptions>>();
            var httpClient = new HttpClient();
            return new DynamicsCrmService(options, httpClient);
        });

        // Microsoft Graph client
        services.AddScoped<GraphServiceClient>(sp =>
        {
            var credential = new DefaultAzureCredential();
            return new GraphServiceClient(credential, new[] { "https://graph.microsoft.com/.default" });
        });

        // Notification services
        services.AddScoped<INotificationService, TeamsNotificationService>();
        services.AddScoped<INotificationService, TeamsChannelNotificationService>();
        services.AddScoped<INotificationService, EmailNotificationService>();

        return services;
    }
}

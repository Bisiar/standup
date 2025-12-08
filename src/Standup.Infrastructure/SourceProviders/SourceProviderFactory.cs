using Microsoft.Extensions.DependencyInjection;
using Standup.Application.Interfaces;
using Standup.Domain.Enums;
using Standup.Domain.Interfaces;

namespace Standup.Infrastructure.SourceProviders;

public class SourceProviderFactory : ISourceProviderFactory
{
    private readonly IServiceProvider _serviceProvider;

    public SourceProviderFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ISourceProvider GetProvider(SourceType sourceType)
    {
        return sourceType switch
        {
            SourceType.GitHub => _serviceProvider.GetRequiredService<GitHubSourceProvider>(),
            SourceType.AzureDevOps => _serviceProvider.GetRequiredService<AzureDevOpsSourceProvider>(),
            _ => throw new ArgumentOutOfRangeException(nameof(sourceType), sourceType, "Unknown source type")
        };
    }
}

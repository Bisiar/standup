using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Standup.Domain.Enums;
using Standup.Infrastructure.SourceProviders;
using Xunit;

namespace Standup.Infrastructure.Tests.SourceProviders;

public sealed class SourceProviderFactoryTests
{
    [Fact]
    public void GetProvider_ForGitHub_ReturnsGitHubSourceProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockGitHubProvider = Substitute.For<GitHubSourceProvider>(null!, null!);
        services.AddSingleton(mockGitHubProvider);

        var serviceProvider = services.BuildServiceProvider();
        var factory = new SourceProviderFactory(serviceProvider);

        // Act
        var provider = factory.GetProvider(SourceType.GitHub);

        // Assert
        provider.Should().Be(mockGitHubProvider);
    }

    [Fact]
    public void GetProvider_ForAzureDevOps_ReturnsAzureDevOpsSourceProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockAdoProvider = Substitute.For<AzureDevOpsSourceProvider>(null!, null!);
        services.AddSingleton(mockAdoProvider);

        var serviceProvider = services.BuildServiceProvider();
        var factory = new SourceProviderFactory(serviceProvider);

        // Act
        var provider = factory.GetProvider(SourceType.AzureDevOps);

        // Assert
        provider.Should().Be(mockAdoProvider);
    }

    [Fact]
    public void GetProvider_ForUnknownSourceType_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var factory = new SourceProviderFactory(serviceProvider);

        // Act
        var act = () => factory.GetProvider((SourceType)999);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Unknown source type*");
    }

    [Fact]
    public void GetProvider_WhenGitHubProviderNotRegistered_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var factory = new SourceProviderFactory(serviceProvider);

        // Act
        var act = () => factory.GetProvider(SourceType.GitHub);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }
}

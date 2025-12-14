using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Standup.Application.Interfaces;
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
        services.AddSingleton<IEncryptionService, TestEncryptionService>();
        services.AddSingleton<GitHubSourceProvider>();

        var serviceProvider = services.BuildServiceProvider();
        var factory = new SourceProviderFactory(serviceProvider);

        // Act
        var provider = factory.GetProvider(SourceType.GitHub);

        // Assert
        provider.Should().BeOfType<GitHubSourceProvider>();
    }

    [Fact]
    public void GetProvider_ForAzureDevOps_ReturnsAzureDevOpsSourceProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IEncryptionService, TestEncryptionService>();
        services.AddSingleton<AzureDevOpsSourceProvider>();

        var serviceProvider = services.BuildServiceProvider();
        var factory = new SourceProviderFactory(serviceProvider);

        // Act
        var provider = factory.GetProvider(SourceType.AzureDevOps);

        // Assert
        provider.Should().BeOfType<AzureDevOpsSourceProvider>();
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

    /// <summary>
    /// Simple test encryption service that doesn't actually encrypt.
    /// Used only for DI registration in factory tests.
    /// </summary>
    private sealed class TestEncryptionService : IEncryptionService
    {
        public string Encrypt(string plainText) => plainText;

        public Task<string> EncryptAsync(string plainText) => Task.FromResult(plainText);

        public Task<string> DecryptAsync(string cipherText) => Task.FromResult(cipherText);
    }
}

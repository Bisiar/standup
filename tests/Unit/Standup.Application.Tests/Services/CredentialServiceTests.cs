using FluentAssertions;
using Standup.Application.Services;
using Standup.Application.Tests.TestHelpers;
using Standup.Domain.Enums;
using Xunit;

namespace Standup.Application.Tests.Services;

/// <summary>
/// Tests for CredentialService - manages organization credentials.
/// NO MOCKS - uses real test implementations.
/// </summary>
public sealed class CredentialServiceTests
{
    private readonly InMemoryCredentialRepository _repository;
    private readonly FakeEncryptionService _encryptionService;
    private readonly CredentialService _service;

    public CredentialServiceTests()
    {
        _repository = new InMemoryCredentialRepository();
        _encryptionService = new FakeEncryptionService();
        _service = new CredentialService(_repository, _encryptionService);
    }

    [Fact]
    public async Task GetCredentialsAsync_ReturnsEmptyList_WhenNoCredentials()
    {
        // Act
        var result = await _service.GetCredentialsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveCredentialAsync_EncryptsPatAndSaves()
    {
        // Arrange
        var plainTextPat = "my-secret-token";

        // Act
        var result = await _service.SaveCredentialAsync(
            SourceType.GitHub,
            "my-org",
            plainTextPat);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeNullOrEmpty();
        result.SourceType.Should().Be(SourceType.GitHub);
        result.Organization.Should().Be("my-org");
        result.EncryptedPat.Should().Be("encrypted:my-secret-token");
    }

    [Fact]
    public async Task SaveCredentialAsync_UpdatesExisting_WhenCredentialExists()
    {
        // Arrange - Save initial credential
        var initial = await _service.SaveCredentialAsync(
            SourceType.AzureDevOps,
            "test-org",
            "initial-token");

        // Act - Save updated credential
        var updated = await _service.SaveCredentialAsync(
            SourceType.AzureDevOps,
            "test-org",
            "updated-token");

        // Assert - Should update, not create new
        updated.Id.Should().Be(initial.Id);
        updated.EncryptedPat.Should().Be("encrypted:updated-token");

        var all = await _service.GetCredentialsAsync();
        all.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetCredentialAsync_ReturnsCredential_WhenExists()
    {
        // Arrange
        await _service.SaveCredentialAsync(
            SourceType.GitHub,
            "github-org",
            "github-token");

        // Act
        var result = await _service.GetCredentialAsync(
            SourceType.GitHub,
            "github-org");

        // Assert
        result.Should().NotBeNull();
        result!.Organization.Should().Be("github-org");
        result.SourceType.Should().Be(SourceType.GitHub);
    }

    [Fact]
    public async Task GetCredentialAsync_ReturnsNull_WhenNotFound()
    {
        // Act
        var result = await _service.GetCredentialAsync(
            SourceType.AzureDevOps,
            "non-existent-org");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCredentialAsync_IsCaseInsensitive_ForOrganization()
    {
        // Arrange
        await _service.SaveCredentialAsync(
            SourceType.GitHub,
            "MyOrg",
            "token");

        // Act
        var result = await _service.GetCredentialAsync(
            SourceType.GitHub,
            "myorg");

        // Assert
        result.Should().NotBeNull();
        result!.Organization.Should().Be("MyOrg");
    }

    [Fact]
    public async Task DeleteCredentialAsync_RemovesCredential()
    {
        // Arrange
        var credential = await _service.SaveCredentialAsync(
            SourceType.GitHub,
            "test-org",
            "token");

        // Act
        await _service.DeleteCredentialAsync(credential.Id);

        // Assert
        var result = await _service.GetCredentialAsync(
            SourceType.GitHub,
            "test-org");
        result.Should().BeNull();
    }

    [Fact]
    public async Task SaveCredentialAsync_SupportMultipleSourceTypes()
    {
        // Arrange & Act - Save credentials for different source types with same org name
        var github = await _service.SaveCredentialAsync(
            SourceType.GitHub,
            "shared-name",
            "github-token");

        var azdo = await _service.SaveCredentialAsync(
            SourceType.AzureDevOps,
            "shared-name",
            "azdo-token");

        // Assert - Both should exist independently
        github.Id.Should().NotBe(azdo.Id);

        var githubResult = await _service.GetCredentialAsync(
            SourceType.GitHub,
            "shared-name");
        githubResult.Should().NotBeNull();
        githubResult!.EncryptedPat.Should().Be("encrypted:github-token");

        var azdoResult = await _service.GetCredentialAsync(
            SourceType.AzureDevOps,
            "shared-name");
        azdoResult.Should().NotBeNull();
        azdoResult!.EncryptedPat.Should().Be("encrypted:azdo-token");
    }

    [Fact]
    public async Task GetCredentialsAsync_ReturnsAllCredentials()
    {
        // Arrange
        await _service.SaveCredentialAsync(SourceType.GitHub, "org1", "token1");
        await _service.SaveCredentialAsync(SourceType.GitHub, "org2", "token2");
        await _service.SaveCredentialAsync(SourceType.AzureDevOps, "org3", "token3");

        // Act
        var result = await _service.GetCredentialsAsync();

        // Assert
        result.Should().HaveCount(3);
    }

    [Theory]
    [InlineData("simple-token", "encrypted:simple-token")]
    [InlineData("complex-token-with-special-chars!@#$%", "encrypted:complex-token-with-special-chars!@#$%")]
    [InlineData("very-long-token-abcdefghijklmnopqrstuvwxyz1234567890", "encrypted:very-long-token-abcdefghijklmnopqrstuvwxyz1234567890")]
    [InlineData("", "")] // Empty string stays empty (FakeEncryptionService behavior)
    public async Task SaveCredentialAsync_HandlesVariousTokenFormats(string token, string expectedEncrypted)
    {
        // Act
        var result = await _service.SaveCredentialAsync(
            SourceType.GitHub,
            "test-org",
            token);

        // Assert
        result.EncryptedPat.Should().Be(expectedEncrypted);
    }
}

using FluentAssertions;
using Microsoft.Extensions.Options;
using Standup.Infrastructure.Configuration;
using Xunit;

namespace Standup.Infrastructure.Tests.Configuration;

public sealed class EncryptionServiceTests
{
    private readonly EncryptionService _service;

    public EncryptionServiceTests()
    {
        var options = Options.Create(new EncryptionOptions
        {
            EncryptionKey = "test-encryption-key-for-unit-tests"
        });
        _service = new EncryptionService(options);
    }

    [Fact]
    public async Task EncryptAsync_ReturnsBase64EncodedString()
    {
        // Arrange
        var plainText = "Hello, World!";

        // Act
        var encrypted = await _service.EncryptAsync(plainText);

        // Assert
        encrypted.Should().NotBeNullOrEmpty();
        encrypted.Should().NotBe(plainText);
        var act = () => Convert.FromBase64String(encrypted);
        act.Should().NotThrow();
    }

    [Fact]
    public async Task DecryptAsync_ReturnsOriginalText()
    {
        // Arrange
        var plainText = "Hello, World!";
        var encrypted = await _service.EncryptAsync(plainText);

        // Act
        var decrypted = await _service.DecryptAsync(encrypted);

        // Assert
        decrypted.Should().Be(plainText);
    }

    [Fact]
    public async Task EncryptAsync_SameTextProducesDifferentCipherText()
    {
        // Arrange
        var plainText = "Hello, World!";

        // Act
        var encrypted1 = await _service.EncryptAsync(plainText);
        var encrypted2 = await _service.EncryptAsync(plainText);

        // Assert
        encrypted1.Should().NotBe(encrypted2);
    }

    [Fact]
    public async Task RoundTrip_WithEmptyString_Works()
    {
        // Arrange
        var plainText = string.Empty;

        // Act
        var encrypted = await _service.EncryptAsync(plainText);
        var decrypted = await _service.DecryptAsync(encrypted);

        // Assert
        decrypted.Should().Be(plainText);
    }

    [Fact]
    public async Task RoundTrip_WithSpecialCharacters_Works()
    {
        // Arrange
        var plainText = "ghp_abc123!@#$%^&*()_+-=[]{}|;':\",./<>?";

        // Act
        var encrypted = await _service.EncryptAsync(plainText);
        var decrypted = await _service.DecryptAsync(encrypted);

        // Assert
        decrypted.Should().Be(plainText);
    }

    [Fact]
    public async Task RoundTrip_WithUnicodeCharacters_Works()
    {
        // Arrange
        var plainText = "Hello, 世界! 🎉";

        // Act
        var encrypted = await _service.EncryptAsync(plainText);
        var decrypted = await _service.DecryptAsync(encrypted);

        // Assert
        decrypted.Should().Be(plainText);
    }

    [Fact]
    public async Task RoundTrip_WithLongText_Works()
    {
        // Arrange
        var plainText = new string('A', 10000);

        // Act
        var encrypted = await _service.EncryptAsync(plainText);
        var decrypted = await _service.DecryptAsync(encrypted);

        // Assert
        decrypted.Should().Be(plainText);
    }

    [Fact]
    public async Task DecryptAsync_WithInvalidBase64_ThrowsException()
    {
        // Arrange
        var invalidCipherText = "not-valid-base64!!!";

        // Act
        var act = () => _service.DecryptAsync(invalidCipherText);

        // Assert
        await act.Should().ThrowAsync<FormatException>();
    }

    [Fact]
    public async Task DifferentKeys_ProduceDifferentCipherText()
    {
        // Arrange
        var options1 = Options.Create(new EncryptionOptions { EncryptionKey = "key-one" });
        var options2 = Options.Create(new EncryptionOptions { EncryptionKey = "key-two" });
        var service1 = new EncryptionService(options1);
        var service2 = new EncryptionService(options2);
        var plainText = "Hello, World!";

        // Act
        var encrypted1 = await service1.EncryptAsync(plainText);
        var encrypted2 = await service2.EncryptAsync(plainText);

        // Assert
        encrypted1.Should().NotBe(encrypted2);
    }

    [Fact]
    public async Task CrossServiceDecryption_Fails()
    {
        // Arrange
        var options1 = Options.Create(new EncryptionOptions { EncryptionKey = "key-one" });
        var options2 = Options.Create(new EncryptionOptions { EncryptionKey = "key-two" });
        var service1 = new EncryptionService(options1);
        var service2 = new EncryptionService(options2);
        var plainText = "Hello, World!";

        var encrypted = await service1.EncryptAsync(plainText);

        // Act
        var act = () => service2.DecryptAsync(encrypted);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }
}

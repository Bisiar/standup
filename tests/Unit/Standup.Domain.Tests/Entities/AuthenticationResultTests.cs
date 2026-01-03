// <copyright file="AuthenticationResultTests.cs" company="Standup">
// Copyright (c) Standup. All rights reserved.
// </copyright>

using FluentAssertions;
using Standup.Domain.Entities;
using Xunit;
using Xunit.Abstractions;

namespace Standup.Domain.Tests.Entities;

/// <summary>
/// Tests for the <see cref="AuthenticationResult"/> entity.
/// </summary>
public sealed class AuthenticationResultTests
{
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationResultTests"/> class.
    /// </summary>
    /// <param name="output">The test output helper.</param>
    public AuthenticationResultTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void AuthenticationResult_Success_CreatesSuccessfulResult()
    {
        // Arrange
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);

        // Act
        var result = AuthenticationResult.Success(
            accessToken: "access-token-123",
            refreshToken: "refresh-token-456",
            expiresAt: expiresAt,
            userEmail: "user@example.com",
            userDisplayName: "John Doe");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.AccessToken.Should().Be("access-token-123");
        result.RefreshToken.Should().Be("refresh-token-456");
        result.ExpiresAt.Should().Be(expiresAt);
        result.UserEmail.Should().Be("user@example.com");
        result.UserDisplayName.Should().Be("John Doe");
        result.ErrorMessage.Should().BeNull();
        _output.WriteLine($"Success for user: {result.UserDisplayName}");
    }

    [Fact]
    public void AuthenticationResult_Failure_CreatesFailedResult()
    {
        // Act
        var result = AuthenticationResult.Failure("Invalid credentials");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.AccessToken.Should().BeNull();
        result.RefreshToken.Should().BeNull();
        result.ExpiresAt.Should().BeNull();
        result.ErrorMessage.Should().Be("Invalid credentials");
        _output.WriteLine($"Failure: {result.ErrorMessage}");
    }

    [Fact]
    public void IsExpired_WhenExpirationInFuture_ReturnsFalse()
    {
        // Arrange
        var result = AuthenticationResult.Success(
            "token",
            "refresh",
            DateTimeOffset.UtcNow.AddHours(1));

        // Act & Assert
        result.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WhenExpirationInPast_ReturnsTrue()
    {
        // Arrange
        var result = AuthenticationResult.Success(
            "token",
            "refresh",
            DateTimeOffset.UtcNow.AddHours(-1));

        // Act & Assert
        result.IsExpired.Should().BeTrue();
        _output.WriteLine("Token is expired");
    }

    [Fact]
    public void IsExpired_WhenExpirationIsNow_ReturnsTrue()
    {
        // Arrange
        var result = AuthenticationResult.Success(
            "token",
            "refresh",
            DateTimeOffset.UtcNow);

        // Act & Assert
        result.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void IsExpired_WhenExpiresAtIsNull_ReturnsFalse()
    {
        // Arrange
        var result = new AuthenticationResult(
            IsSuccess: true,
            AccessToken: "token",
            ExpiresAt: null);

        // Act & Assert
        result.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void IsFailure_WhenSuccess_ReturnsFalse()
    {
        // Arrange
        var result = AuthenticationResult.Success("token", null, DateTimeOffset.UtcNow.AddHours(1));

        // Act & Assert
        result.IsFailure.Should().BeFalse();
    }

    [Fact]
    public void IsFailure_WhenFailed_ReturnsTrue()
    {
        // Arrange
        var result = AuthenticationResult.Failure("Error");

        // Act & Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Success_WithMinimalParameters_Works()
    {
        // Act
        var result = AuthenticationResult.Success(
            accessToken: "token",
            refreshToken: null,
            expiresAt: DateTimeOffset.UtcNow.AddHours(1));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.AccessToken.Should().Be("token");
        result.RefreshToken.Should().BeNull();
        result.UserEmail.Should().BeNull();
        result.UserDisplayName.Should().BeNull();
    }

    [Fact]
    public void AuthenticationResult_DirectConstruction_Works()
    {
        // Act
        var result = new AuthenticationResult(
            IsSuccess: true,
            AccessToken: "token",
            RefreshToken: "refresh",
            ExpiresAt: DateTimeOffset.UtcNow.AddHours(1),
            UserEmail: "user@test.com",
            UserDisplayName: "Test User",
            ErrorMessage: null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.AccessToken.Should().Be("token");
        result.RefreshToken.Should().Be("refresh");
        result.UserEmail.Should().Be("user@test.com");
        result.UserDisplayName.Should().Be("Test User");
    }

    [Fact]
    public void AuthenticationResult_RecordEquality_WorksCorrectly()
    {
        // Arrange
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);
        var result1 = AuthenticationResult.Success("token", "refresh", expiresAt);
        var result2 = AuthenticationResult.Success("token", "refresh", expiresAt);
        var result3 = AuthenticationResult.Success("different", "refresh", expiresAt);

        // Act & Assert
        result1.Should().Be(result2);
        result1.Should().NotBe(result3);
    }
}

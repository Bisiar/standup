using FluentAssertions;
using Standup.Application.DTOs;
using Standup.Domain.Entities;
using Xunit;

namespace Standup.Application.Tests.DTOs;

public class UserDtoTests
{
    [Fact]
    public void UserDto_CanBeCreated()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var preferences = new UserPreferences();

        // Act
        var dto = new UserDto(
            Id: "user-123",
            DisplayName: "John Doe",
            Email: "john@example.com",
            TenantId: "tenant-1",
            Preferences: preferences,
            IsActive: true,
            CreatedAt: now,
            LastStandupAt: null);

        // Assert
        dto.Id.Should().Be("user-123");
        dto.DisplayName.Should().Be("John Doe");
        dto.Email.Should().Be("john@example.com");
        dto.TenantId.Should().Be("tenant-1");
        dto.Preferences.Should().Be(preferences);
        dto.IsActive.Should().BeTrue();
        dto.CreatedAt.Should().Be(now);
        dto.LastStandupAt.Should().BeNull();
    }

    [Fact]
    public void UserDto_WithLastStandupAt_StoresTimestamp()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow.AddDays(-7);
        var lastStandupAt = DateTimeOffset.UtcNow.AddHours(-2);
        var preferences = new UserPreferences();

        // Act
        var dto = new UserDto(
            Id: "user-123",
            DisplayName: "Jane Smith",
            Email: "jane@example.com",
            TenantId: "tenant-1",
            Preferences: preferences,
            IsActive: true,
            CreatedAt: createdAt,
            LastStandupAt: lastStandupAt);

        // Assert
        dto.LastStandupAt.Should().Be(lastStandupAt);
        dto.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void UserDto_IsRecord_SupportsEquality()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var preferences = new UserPreferences();

        var dto1 = new UserDto(
            Id: "user-1",
            DisplayName: "User",
            Email: "user@test.com",
            TenantId: "tenant-1",
            Preferences: preferences,
            IsActive: true,
            CreatedAt: now,
            LastStandupAt: null);

        var dto2 = new UserDto(
            Id: "user-1",
            DisplayName: "User",
            Email: "user@test.com",
            TenantId: "tenant-1",
            Preferences: preferences,
            IsActive: true,
            CreatedAt: now,
            LastStandupAt: null);

        // Act & Assert
        dto1.Should().Be(dto2);
    }

    [Fact]
    public void UserDto_DifferentValues_AreNotEqual()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var preferences = new UserPreferences();

        var dto1 = new UserDto("user-1", "Name A", "a@test.com", "tenant-1", preferences, true, now, null);
        var dto2 = new UserDto("user-1", "Name B", "a@test.com", "tenant-1", preferences, true, now, null);

        // Act & Assert
        dto1.Should().NotBe(dto2);
    }

    [Fact]
    public void UserDto_SupportsDeconstruction()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var lastStandup = DateTimeOffset.UtcNow.AddHours(-1);
        var preferences = new UserPreferences();

        var dto = new UserDto(
            Id: "user-1",
            DisplayName: "Test User",
            Email: "test@example.com",
            TenantId: "tenant-1",
            Preferences: preferences,
            IsActive: true,
            CreatedAt: now,
            LastStandupAt: lastStandup);

        // Act
        var (id, displayName, email, tenantId, prefs, isActive, createdAt, lastStandupAt) = dto;

        // Assert
        id.Should().Be("user-1");
        displayName.Should().Be("Test User");
        email.Should().Be("test@example.com");
        tenantId.Should().Be("tenant-1");
        prefs.Should().Be(preferences);
        isActive.Should().BeTrue();
        createdAt.Should().Be(now);
        lastStandupAt.Should().Be(lastStandup);
    }

    [Fact]
    public void UserDto_SupportsWithExpression()
    {
        // Arrange
        var preferences = new UserPreferences();
        var original = new UserDto(
            Id: "user-1",
            DisplayName: "Original Name",
            Email: "original@test.com",
            TenantId: "tenant-1",
            Preferences: preferences,
            IsActive: true,
            CreatedAt: DateTimeOffset.UtcNow,
            LastStandupAt: null);

        // Act
        var modified = original with { DisplayName = "New Name", Email = "new@test.com" };

        // Assert
        modified.DisplayName.Should().Be("New Name");
        modified.Email.Should().Be("new@test.com");
        modified.Id.Should().Be(original.Id);
        modified.TenantId.Should().Be(original.TenantId);
        modified.Preferences.Should().Be(original.Preferences);
    }

    [Fact]
    public void UserDto_WithInactiveUser_HasIsActiveFalse()
    {
        // Arrange
        var preferences = new UserPreferences();

        // Act
        var dto = new UserDto(
            Id: "user-inactive",
            DisplayName: "Inactive User",
            Email: "inactive@test.com",
            TenantId: "tenant-1",
            Preferences: preferences,
            IsActive: false,
            CreatedAt: DateTimeOffset.UtcNow,
            LastStandupAt: null);

        // Assert
        dto.IsActive.Should().BeFalse();
    }

    [Fact]
    public void UserDto_WithCustomPreferences_StoresPreferences()
    {
        // Arrange
        var preferences = new UserPreferences
        {
            LookbackHours = 48,
            TimeZone = "America/New_York",
            SkipWeekends = true,
        };

        // Act
        var dto = new UserDto(
            Id: "user-1",
            DisplayName: "User",
            Email: "user@test.com",
            TenantId: "tenant-1",
            Preferences: preferences,
            IsActive: true,
            CreatedAt: DateTimeOffset.UtcNow,
            LastStandupAt: null);

        // Assert
        dto.Preferences.LookbackHours.Should().Be(48);
        dto.Preferences.TimeZone.Should().Be("America/New_York");
        dto.Preferences.SkipWeekends.Should().BeTrue();
    }
}

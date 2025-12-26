using FluentAssertions;
using Standup.Domain.Entities;
using Standup.Domain.Enums;
using Xunit;

namespace Standup.Domain.Tests.Entities;

public class UserPreferencesTests
{
    [Fact]
    public void UserPreferences_HasDefaultValues()
    {
        // Act
        var preferences = new UserPreferences();

        // Assert
        preferences.NotificationChannels.Should().HaveCount(1);
        preferences.NotificationChannels.Should().Contain(NotificationChannel.TeamsDirectMessage);
        preferences.TimeZone.Should().Be("America/Denver");
        preferences.IncludeCommitDetails.Should().BeTrue();
        preferences.IncludePullRequests.Should().BeTrue();
        preferences.IncludeWorkItems.Should().BeTrue();
        preferences.LookbackHours.Should().Be(24);
        preferences.SkipWeekends.Should().BeTrue();
    }

    [Fact]
    public void UserPreferences_CanSetCustomValues()
    {
        // Arrange
        var channels = new List<NotificationChannel>
        {
            NotificationChannel.Email,
            NotificationChannel.TeamsChannel
        };

        // Act
        var preferences = new UserPreferences(
            NotificationChannels: channels,
            TimeZone: "America/New_York",
            IncludeCommitDetails: false,
            IncludePullRequests: false,
            IncludeWorkItems: false,
            LookbackHours: 48,
            SkipWeekends: false);

        // Assert
        preferences.NotificationChannels.Should().HaveCount(2);
        preferences.NotificationChannels.Should().Contain(NotificationChannel.Email);
        preferences.NotificationChannels.Should().Contain(NotificationChannel.TeamsChannel);
        preferences.TimeZone.Should().Be("America/New_York");
        preferences.IncludeCommitDetails.Should().BeFalse();
        preferences.IncludePullRequests.Should().BeFalse();
        preferences.IncludeWorkItems.Should().BeFalse();
        preferences.LookbackHours.Should().Be(48);
        preferences.SkipWeekends.Should().BeFalse();
    }

    [Fact]
    public void UserPreferences_WithNullChannels_UsesDefaultChannel()
    {
        // Act
        var preferences = new UserPreferences(NotificationChannels: null);

        // Assert
        preferences.NotificationChannels.Should().NotBeNull();
        preferences.NotificationChannels.Should().HaveCount(1);
        preferences.NotificationChannels.Should().Contain(NotificationChannel.TeamsDirectMessage);
    }

    [Fact]
    public void UserPreferences_IsRecord_SupportsEquality()
    {
        // Arrange
        var channels = new List<NotificationChannel> { NotificationChannel.Email };

        var pref1 = new UserPreferences(
            NotificationChannels: channels,
            TimeZone: "UTC",
            LookbackHours: 12);

        var pref2 = new UserPreferences(
            NotificationChannels: channels,
            TimeZone: "UTC",
            LookbackHours: 12);

        // Assert
        pref1.Should().Be(pref2);
        (pref1 == pref2).Should().BeTrue();
    }

    [Fact]
    public void UserPreferences_DifferentValues_AreNotEqual()
    {
        // Arrange
        var pref1 = new UserPreferences(TimeZone: "America/Denver");
        var pref2 = new UserPreferences(TimeZone: "America/New_York");

        // Assert
        pref1.Should().NotBe(pref2);
        (pref1 != pref2).Should().BeTrue();
    }

    [Fact]
    public void UserPreferences_SupportsDeconstruction()
    {
        // Arrange
        var channels = new List<NotificationChannel>
        {
            NotificationChannel.TeamsChannel,
            NotificationChannel.Email
        };

        var preferences = new UserPreferences(
            NotificationChannels: channels,
            TimeZone: "Europe/London",
            IncludeCommitDetails: false,
            IncludePullRequests: true,
            IncludeWorkItems: true,
            LookbackHours: 72,
            SkipWeekends: false);

        // Act
        var (notificationChannels, timeZone, includeCommitDetails, includePullRequests,
             includeWorkItems, lookbackHours, skipWeekends) = preferences;

        // Assert
        notificationChannels.Should().HaveCount(2);
        timeZone.Should().Be("Europe/London");
        includeCommitDetails.Should().BeFalse();
        includePullRequests.Should().BeTrue();
        includeWorkItems.Should().BeTrue();
        lookbackHours.Should().Be(72);
        skipWeekends.Should().BeFalse();
    }

    [Theory]
    [InlineData("America/New_York")]
    [InlineData("America/Chicago")]
    [InlineData("America/Los_Angeles")]
    [InlineData("Europe/London")]
    [InlineData("Asia/Tokyo")]
    [InlineData("UTC")]
    public void UserPreferences_SupportsDifferentTimeZones(string timeZone)
    {
        // Act
        var preferences = new UserPreferences(TimeZone: timeZone);

        // Assert
        preferences.TimeZone.Should().Be(timeZone);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(12)]
    [InlineData(24)]
    [InlineData(48)]
    [InlineData(168)] // One week
    public void UserPreferences_SupportsDifferentLookbackHours(int hours)
    {
        // Act
        var preferences = new UserPreferences(LookbackHours: hours);

        // Assert
        preferences.LookbackHours.Should().Be(hours);
    }

    [Fact]
    public void UserPreferences_CanHaveMultipleNotificationChannels()
    {
        // Arrange
        var channels = new List<NotificationChannel>
        {
            NotificationChannel.TeamsDirectMessage,
            NotificationChannel.TeamsChannel,
            NotificationChannel.Email
        };

        // Act
        var preferences = new UserPreferences(NotificationChannels: channels);

        // Assert
        preferences.NotificationChannels.Should().HaveCount(3);
        preferences.NotificationChannels.Should().Contain(NotificationChannel.TeamsDirectMessage);
        preferences.NotificationChannels.Should().Contain(NotificationChannel.TeamsChannel);
        preferences.NotificationChannels.Should().Contain(NotificationChannel.Email);
    }

    [Fact]
    public void UserPreferences_WithAllIncludesEnabled_ShowsAllData()
    {
        // Act
        var preferences = new UserPreferences(
            IncludeCommitDetails: true,
            IncludePullRequests: true,
            IncludeWorkItems: true);

        // Assert
        preferences.IncludeCommitDetails.Should().BeTrue();
        preferences.IncludePullRequests.Should().BeTrue();
        preferences.IncludeWorkItems.Should().BeTrue();
    }

    [Fact]
    public void UserPreferences_WithAllIncludesDisabled_ShowsMinimalData()
    {
        // Act
        var preferences = new UserPreferences(
            IncludeCommitDetails: false,
            IncludePullRequests: false,
            IncludeWorkItems: false);

        // Assert
        preferences.IncludeCommitDetails.Should().BeFalse();
        preferences.IncludePullRequests.Should().BeFalse();
        preferences.IncludeWorkItems.Should().BeFalse();
    }

    [Fact]
    public void UserPreferences_WithSkipWeekends_ExcludesWeekendActivity()
    {
        // Act
        var preferences = new UserPreferences(SkipWeekends: true);

        // Assert
        preferences.SkipWeekends.Should().BeTrue();
    }

    [Fact]
    public void UserPreferences_WithoutSkipWeekends_IncludesWeekendActivity()
    {
        // Act
        var preferences = new UserPreferences(SkipWeekends: false);

        // Assert
        preferences.SkipWeekends.Should().BeFalse();
    }

    [Fact]
    public void UserPreferences_SupportsWithExpression()
    {
        // Arrange
        var original = new UserPreferences(
            TimeZone: "America/Denver",
            LookbackHours: 24);

        // Act
        var updated = original with
        {
            TimeZone = "America/New_York",
            LookbackHours = 48
        };

        // Assert
        updated.TimeZone.Should().Be("America/New_York");
        updated.LookbackHours.Should().Be(48);
        updated.IncludeCommitDetails.Should().Be(original.IncludeCommitDetails);
        original.TimeZone.Should().Be("America/Denver"); // Original unchanged
        original.LookbackHours.Should().Be(24);
    }

    [Fact]
    public void UserPreferences_WithSingleChannel_CanBeConfigured()
    {
        // Arrange
        var channels = new List<NotificationChannel> { NotificationChannel.Email };

        // Act
        var preferences = new UserPreferences(NotificationChannels: channels);

        // Assert
        preferences.NotificationChannels.Should().HaveCount(1);
        preferences.NotificationChannels.Should().Contain(NotificationChannel.Email);
    }
}

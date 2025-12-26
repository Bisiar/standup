using FluentAssertions;
using Standup.Domain.Entities;
using Xunit;

namespace Standup.Domain.Tests.Entities;

public sealed class UserTests
{
    [Fact]
    public void NewUser_HasDefaultValues()
    {
        // Act
        var user = new User();

        // Assert
        user.Id.Should().NotBeNullOrEmpty();
        user.TenantId.Should().BeEmpty();
        user.EntraUserId.Should().BeEmpty();
        user.DisplayName.Should().BeEmpty();
        user.Email.Should().BeEmpty();
        user.TeamsUserId.Should().BeNull();
        user.IsActive.Should().BeTrue();
        user.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        user.LastStandupAt.Should().BeNull();
        user.Tenant.Should().BeNull();
        user.SourceRepositories.Should().BeEmpty();
        user.StandupReports.Should().BeEmpty();
        user.Subscriptions.Should().BeEmpty();
    }

    [Fact]
    public void NewUser_HasDefaultPreferences()
    {
        // Act
        var user = new User();

        // Assert
        user.Preferences.Should().NotBeNull();
    }

    [Fact]
    public void User_CanSetProperties()
    {
        // Arrange
        var user = new User();
        var now = DateTimeOffset.UtcNow;

        // Act
        user.TenantId = "tenant-123";
        user.EntraUserId = "entra-user-456";
        user.DisplayName = "John Doe";
        user.Email = "john.doe@company.com";
        user.TeamsUserId = "teams-user-789";
        user.IsActive = false;
        user.LastStandupAt = now;

        // Assert
        user.TenantId.Should().Be("tenant-123");
        user.EntraUserId.Should().Be("entra-user-456");
        user.DisplayName.Should().Be("John Doe");
        user.Email.Should().Be("john.doe@company.com");
        user.TeamsUserId.Should().Be("teams-user-789");
        user.IsActive.Should().BeFalse();
        user.LastStandupAt.Should().Be(now);
    }

    [Fact]
    public void User_CanHaveTenant()
    {
        // Arrange
        var tenant = new Tenant { Name = "JourneyTeam" };
        var user = new User { DisplayName = "John Doe" };

        // Act
        user.Tenant = tenant;
        user.TenantId = tenant.Id;

        // Assert
        user.Tenant.Should().Be(tenant);
        user.TenantId.Should().Be(tenant.Id);
    }

    [Fact]
    public void User_CanAddSourceRepositories()
    {
        // Arrange
        var user = new User { DisplayName = "John Doe" };
        var repo = new SourceRepository { Repository = "my-repo" };

        // Act
        user.SourceRepositories.Add(repo);

        // Assert
        user.SourceRepositories.Should().HaveCount(1);
        user.SourceRepositories.Should().Contain(repo);
    }

    [Fact]
    public void User_CanAddStandupReports()
    {
        // Arrange
        var user = new User { DisplayName = "John Doe" };
        var report = new StandupReport { Summary = "Did some work" };

        // Act
        user.StandupReports.Add(report);

        // Assert
        user.StandupReports.Should().HaveCount(1);
        user.StandupReports.Should().Contain(report);
    }

    [Fact]
    public void User_CanAddSubscriptions()
    {
        // Arrange
        var user = new User { DisplayName = "John Doe" };
        var subscription = new TeamSubscription { ChannelId = "conv-123" };

        // Act
        user.Subscriptions.Add(subscription);

        // Assert
        user.Subscriptions.Should().HaveCount(1);
        user.Subscriptions.Should().Contain(subscription);
    }

    [Fact]
    public void User_GeneratesUniqueIds()
    {
        // Arrange & Act
        var user1 = new User();
        var user2 = new User();

        // Assert
        user1.Id.Should().NotBe(user2.Id);
    }
}

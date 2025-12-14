using FluentAssertions;
using Standup.Domain.Entities;
using Xunit;

namespace Standup.Domain.Tests.Entities;

public sealed class TenantTests
{
    [Fact]
    public void NewTenant_HasDefaultValues()
    {
        // Act
        var tenant = new Tenant();

        // Assert
        tenant.Id.Should().NotBeNullOrEmpty();
        tenant.Name.Should().BeEmpty();
        tenant.EntraTenantId.Should().BeEmpty();
        tenant.TeamsTeamId.Should().BeNull();
        tenant.DefaultChannelId.Should().BeNull();
        tenant.IsActive.Should().BeTrue();
        tenant.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        tenant.UpdatedAt.Should().BeNull();
        tenant.Users.Should().BeEmpty();
        tenant.Schedules.Should().BeEmpty();
    }

    [Fact]
    public void NewTenant_HasDefaultEmailSettings()
    {
        // Act
        var tenant = new Tenant();

        // Assert
        tenant.EmailSettings.Should().NotBeNull();
    }

    [Fact]
    public void NewTenant_HasDefaultAISettings()
    {
        // Act
        var tenant = new Tenant();

        // Assert
        tenant.AISettings.Should().NotBeNull();
    }

    [Fact]
    public void Tenant_CanSetProperties()
    {
        // Arrange
        var tenant = new Tenant();
        var now = DateTimeOffset.UtcNow;

        // Act
        tenant.Name = "JourneyTeam";
        tenant.EntraTenantId = "tenant-123";
        tenant.TeamsTeamId = "team-456";
        tenant.DefaultChannelId = "channel-789";
        tenant.IsActive = false;
        tenant.UpdatedAt = now;

        // Assert
        tenant.Name.Should().Be("JourneyTeam");
        tenant.EntraTenantId.Should().Be("tenant-123");
        tenant.TeamsTeamId.Should().Be("team-456");
        tenant.DefaultChannelId.Should().Be("channel-789");
        tenant.IsActive.Should().BeFalse();
        tenant.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public void Tenant_CanAddUsers()
    {
        // Arrange
        var tenant = new Tenant { Name = "JourneyTeam" };
        var user = new User { DisplayName = "John Doe" };

        // Act
        tenant.Users.Add(user);

        // Assert
        tenant.Users.Should().HaveCount(1);
        tenant.Users.Should().Contain(user);
    }

    [Fact]
    public void Tenant_CanAddSchedules()
    {
        // Arrange
        var tenant = new Tenant { Name = "JourneyTeam" };
        var schedule = new StandupSchedule { Name = "Morning Standup" };

        // Act
        tenant.Schedules.Add(schedule);

        // Assert
        tenant.Schedules.Should().HaveCount(1);
        tenant.Schedules.Should().Contain(schedule);
    }

    [Fact]
    public void Tenant_GeneratesUniqueIds()
    {
        // Arrange & Act
        var tenant1 = new Tenant();
        var tenant2 = new Tenant();

        // Assert
        tenant1.Id.Should().NotBe(tenant2.Id);
    }
}

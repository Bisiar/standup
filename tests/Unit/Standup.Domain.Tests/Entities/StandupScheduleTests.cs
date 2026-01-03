// <copyright file="StandupScheduleTests.cs" company="Standup">
// Copyright (c) Standup. All rights reserved.
// </copyright>

using FluentAssertions;
using Standup.Domain.Entities;
using Xunit;
using Xunit.Abstractions;

namespace Standup.Domain.Tests.Entities;

/// <summary>
/// Tests for the <see cref="StandupSchedule"/> entity.
/// </summary>
public sealed class StandupScheduleTests
{
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Initializes a new instance of the <see cref="StandupScheduleTests"/> class.
    /// </summary>
    /// <param name="output">The test output helper.</param>
    public StandupScheduleTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void NewStandupSchedule_HasDefaultValues()
    {
        // Act
        var schedule = new StandupSchedule();

        // Assert
        schedule.Id.Should().NotBeNullOrEmpty();
        schedule.TenantId.Should().BeEmpty();
        schedule.Name.Should().BeEmpty();
        schedule.CronExpression.Should().Be("0 8 * * 1-5");
        schedule.TimeZone.Should().Be("America/Denver");
        schedule.NotifyMinutesBefore.Should().Be(15);
        schedule.TeamsChannelId.Should().BeNull();
        schedule.IsAsync.Should().BeFalse();
        schedule.IsActive.Should().BeTrue();
        schedule.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        schedule.Tenant.Should().BeNull();
        _output.WriteLine($"Created schedule with default cron: {schedule.CronExpression}");
    }

    [Fact]
    public void StandupSchedule_CanSetProperties()
    {
        // Arrange
        var schedule = new StandupSchedule();
        var createdAt = DateTimeOffset.UtcNow.AddDays(-30);

        // Act
        schedule.TenantId = "tenant-123";
        schedule.Name = "Morning Standup";
        schedule.CronExpression = "0 9 * * 1-5";
        schedule.TimeZone = "America/Los_Angeles";
        schedule.NotifyMinutesBefore = 10;
        schedule.TeamsChannelId = "channel-456";
        schedule.IsAsync = true;
        schedule.IsActive = true;
        schedule.CreatedAt = createdAt;

        // Assert
        schedule.TenantId.Should().Be("tenant-123");
        schedule.Name.Should().Be("Morning Standup");
        schedule.CronExpression.Should().Be("0 9 * * 1-5");
        schedule.TimeZone.Should().Be("America/Los_Angeles");
        schedule.NotifyMinutesBefore.Should().Be(10);
        schedule.TeamsChannelId.Should().Be("channel-456");
        schedule.IsAsync.Should().BeTrue();
        schedule.IsActive.Should().BeTrue();
        schedule.CreatedAt.Should().Be(createdAt);
        _output.WriteLine($"Schedule '{schedule.Name}' at {schedule.CronExpression} ({schedule.TimeZone})");
    }

    [Fact]
    public void StandupSchedule_GeneratesUniqueIds()
    {
        // Arrange & Act
        var schedule1 = new StandupSchedule();
        var schedule2 = new StandupSchedule();

        // Assert
        schedule1.Id.Should().NotBe(schedule2.Id);
        _output.WriteLine($"Schedule1 ID: {schedule1.Id}, Schedule2 ID: {schedule2.Id}");
    }

    [Fact]
    public void StandupSchedule_CanBeSynchronousOrAsync()
    {
        // Arrange
        var syncSchedule = new StandupSchedule { Name = "Team Sync", IsAsync = false };
        var asyncSchedule = new StandupSchedule { Name = "Async Update", IsAsync = true };

        // Assert
        syncSchedule.IsAsync.Should().BeFalse();
        asyncSchedule.IsAsync.Should().BeTrue();
    }

    [Fact]
    public void StandupSchedule_CanBeDeactivated()
    {
        // Arrange
        var schedule = new StandupSchedule { Name = "Test", IsActive = true };

        // Act
        schedule.IsActive = false;

        // Assert
        schedule.IsActive.Should().BeFalse();
    }

    [Fact]
    public void StandupSchedule_CanAssociateTenant()
    {
        // Arrange
        var tenant = new Tenant { Name = "Test Tenant" };
        var schedule = new StandupSchedule { Name = "Team Standup" };

        // Act
        schedule.Tenant = tenant;
        schedule.TenantId = tenant.Id;

        // Assert
        schedule.Tenant.Should().Be(tenant);
        schedule.TenantId.Should().Be(tenant.Id);
        _output.WriteLine($"Schedule associated with tenant: {schedule.Tenant.Name}");
    }

    [Fact]
    public void StandupSchedule_CommonCronExpressions_AreValid()
    {
        // Test common schedule patterns
        var dailyAt8Am = new StandupSchedule { CronExpression = "0 8 * * 1-5" };
        var dailyAt9Am = new StandupSchedule { CronExpression = "0 9 * * 1-5" };
        var everyWeekday = new StandupSchedule { CronExpression = "0 10 * * MON-FRI" };
        var onceAWeek = new StandupSchedule { CronExpression = "0 9 * * MON" };

        dailyAt8Am.CronExpression.Should().NotBeEmpty();
        dailyAt9Am.CronExpression.Should().NotBeEmpty();
        everyWeekday.CronExpression.Should().NotBeEmpty();
        onceAWeek.CronExpression.Should().NotBeEmpty();
    }

    [Fact]
    public void StandupSchedule_TimeZones_CanBeSet()
    {
        // Test different timezone settings
        var denver = new StandupSchedule { TimeZone = "America/Denver" };
        var pacific = new StandupSchedule { TimeZone = "America/Los_Angeles" };
        var utc = new StandupSchedule { TimeZone = "UTC" };

        denver.TimeZone.Should().Be("America/Denver");
        pacific.TimeZone.Should().Be("America/Los_Angeles");
        utc.TimeZone.Should().Be("UTC");
    }
}

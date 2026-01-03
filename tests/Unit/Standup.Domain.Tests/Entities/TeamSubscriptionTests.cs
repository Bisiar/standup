// <copyright file="TeamSubscriptionTests.cs" company="Standup">
// Copyright (c) Standup. All rights reserved.
// </copyright>

using FluentAssertions;
using Standup.Domain.Entities;
using Xunit;
using Xunit.Abstractions;

namespace Standup.Domain.Tests.Entities;

/// <summary>
/// Tests for the <see cref="TeamSubscription"/> entity.
/// </summary>
public sealed class TeamSubscriptionTests
{
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Initializes a new instance of the <see cref="TeamSubscriptionTests"/> class.
    /// </summary>
    /// <param name="output">The test output helper.</param>
    public TeamSubscriptionTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void NewTeamSubscription_HasDefaultValues()
    {
        // Act
        var subscription = new TeamSubscription();

        // Assert
        subscription.Id.Should().NotBeNullOrEmpty();
        subscription.UserId.Should().BeEmpty();
        subscription.TenantId.Should().BeEmpty();
        subscription.TeamId.Should().BeEmpty();
        subscription.ChannelId.Should().BeEmpty();
        subscription.ChannelName.Should().BeEmpty();
        subscription.AutoPost.Should().BeTrue();
        subscription.IsActive.Should().BeTrue();
        subscription.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        subscription.User.Should().BeNull();
        _output.WriteLine($"Created subscription with ID: {subscription.Id}");
    }

    [Fact]
    public void TeamSubscription_CanSetProperties()
    {
        // Arrange
        var subscription = new TeamSubscription();
        var createdAt = DateTimeOffset.UtcNow.AddDays(-10);

        // Act
        subscription.UserId = "user-123";
        subscription.TenantId = "tenant-456";
        subscription.TeamId = "team-789";
        subscription.ChannelId = "channel-101";
        subscription.ChannelName = "General";
        subscription.AutoPost = false;
        subscription.IsActive = true;
        subscription.CreatedAt = createdAt;

        // Assert
        subscription.UserId.Should().Be("user-123");
        subscription.TenantId.Should().Be("tenant-456");
        subscription.TeamId.Should().Be("team-789");
        subscription.ChannelId.Should().Be("channel-101");
        subscription.ChannelName.Should().Be("General");
        subscription.AutoPost.Should().BeFalse();
        subscription.IsActive.Should().BeTrue();
        subscription.CreatedAt.Should().Be(createdAt);
        _output.WriteLine($"Subscription for channel: {subscription.ChannelName}");
    }

    [Fact]
    public void TeamSubscription_GeneratesUniqueIds()
    {
        // Arrange & Act
        var sub1 = new TeamSubscription();
        var sub2 = new TeamSubscription();

        // Assert
        sub1.Id.Should().NotBe(sub2.Id);
        _output.WriteLine($"Sub1 ID: {sub1.Id}, Sub2 ID: {sub2.Id}");
    }

    [Fact]
    public void TeamSubscription_CanBeDeactivated()
    {
        // Arrange
        var subscription = new TeamSubscription { IsActive = true };

        // Act
        subscription.IsActive = false;

        // Assert
        subscription.IsActive.Should().BeFalse();
    }

    [Fact]
    public void TeamSubscription_AutoPost_CanBeDisabled()
    {
        // Arrange
        var subscription = new TeamSubscription { AutoPost = true };

        // Act
        subscription.AutoPost = false;

        // Assert
        subscription.AutoPost.Should().BeFalse();
    }

    [Fact]
    public void TeamSubscription_CanAssociateUser()
    {
        // Arrange
        var user = new User { DisplayName = "John Doe" };
        var subscription = new TeamSubscription { UserId = user.Id };

        // Act
        subscription.User = user;

        // Assert
        subscription.User.Should().Be(user);
        subscription.UserId.Should().Be(user.Id);
        _output.WriteLine($"Subscription for user: {subscription.User.DisplayName}");
    }
}

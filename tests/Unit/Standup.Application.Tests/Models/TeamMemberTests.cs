// <copyright file="TeamMemberTests.cs" company="Standup">
// Copyright (c) Standup. All rights reserved.
// </copyright>

using FluentAssertions;
using Standup.Application.Models;
using Xunit;
using Xunit.Abstractions;

namespace Standup.Application.Tests.Models;

/// <summary>
/// Tests for the <see cref="TeamMember"/> record.
/// </summary>
public sealed class TeamMemberTests
{
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Initializes a new instance of the <see cref="TeamMemberTests"/> class.
    /// </summary>
    /// <param name="output">The test output helper.</param>
    public TeamMemberTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void TeamMember_WithRequiredProperties_SetsValuesCorrectly()
    {
        // Act
        var member = new TeamMember(
            Name: "John Doe",
            Initials: "JD",
            Role: "Senior Developer",
            RoleType: TeamRole.Developer,
            TaskCount: 5,
            Status: MemberStatus.Active);

        // Assert
        member.Name.Should().Be("John Doe");
        member.Initials.Should().Be("JD");
        member.Role.Should().Be("Senior Developer");
        member.RoleType.Should().Be(TeamRole.Developer);
        member.TaskCount.Should().Be(5);
        member.Status.Should().Be(MemberStatus.Active);
        _output.WriteLine($"Team member: {member.Name} ({member.Role})");
    }

    [Fact]
    public void AvatarColor_ForLeadRole_ReturnsBlueColor()
    {
        // Arrange
        var member = new TeamMember("Lead", "LD", "Team Lead", TeamRole.Lead, 10, MemberStatus.Active);

        // Act & Assert
        member.AvatarColor.Should().Be("#3B82F6");
        _output.WriteLine($"Lead avatar color: {member.AvatarColor}");
    }

    [Fact]
    public void AvatarColor_ForDeveloperRole_ReturnsGreenColor()
    {
        // Arrange
        var member = new TeamMember("Dev", "DV", "Developer", TeamRole.Developer, 5, MemberStatus.Active);

        // Act & Assert
        member.AvatarColor.Should().Be("#10B981");
    }

    [Fact]
    public void AvatarColor_ForQARole_ReturnsAmberColor()
    {
        // Arrange
        var member = new TeamMember("QA", "QA", "QA Engineer", TeamRole.QA, 3, MemberStatus.Active);

        // Act & Assert
        member.AvatarColor.Should().Be("#F59E0B");
    }

    [Fact]
    public void AvatarColor_ForDevOpsRole_ReturnsPurpleColor()
    {
        // Arrange
        var member = new TeamMember("DevOps", "DO", "DevOps Engineer", TeamRole.DevOps, 2, MemberStatus.Active);

        // Act & Assert
        member.AvatarColor.Should().Be("#8B5CF6");
    }

    [Fact]
    public void StatusColor_ForActiveStatus_ReturnsGreenColor()
    {
        // Arrange
        var member = new TeamMember("John", "JD", "Dev", TeamRole.Developer, 5, MemberStatus.Active);

        // Act & Assert
        member.StatusColor.Should().Be("#22C55E");
        _output.WriteLine($"Active status color: {member.StatusColor}");
    }

    [Fact]
    public void StatusColor_ForAwayStatus_ReturnsAmberColor()
    {
        // Arrange
        var member = new TeamMember("John", "JD", "Dev", TeamRole.Developer, 5, MemberStatus.Away);

        // Act & Assert
        member.StatusColor.Should().Be("#F59E0B");
    }

    [Fact]
    public void StatusColor_ForOfflineStatus_ReturnsGrayColor()
    {
        // Arrange
        var member = new TeamMember("John", "JD", "Dev", TeamRole.Developer, 5, MemberStatus.Offline);

        // Act & Assert
        member.StatusColor.Should().Be("#64748B");
    }

    [Theory]
    [InlineData(MemberStatus.Active, "Active")]
    [InlineData(MemberStatus.Away, "Away")]
    [InlineData(MemberStatus.Offline, "Offline")]
    public void StatusText_ReturnsCorrectString(MemberStatus status, string expectedText)
    {
        // Arrange
        var member = new TeamMember("John", "JD", "Dev", TeamRole.Developer, 5, status);

        // Act & Assert
        member.StatusText.Should().Be(expectedText);
    }

    [Fact]
    public void TeamMember_RecordEquality_WorksCorrectly()
    {
        // Arrange
        var member1 = new TeamMember("John", "JD", "Dev", TeamRole.Developer, 5, MemberStatus.Active);
        var member2 = new TeamMember("John", "JD", "Dev", TeamRole.Developer, 5, MemberStatus.Active);
        var member3 = new TeamMember("Jane", "JD", "Dev", TeamRole.Developer, 5, MemberStatus.Active);

        // Act & Assert
        member1.Should().Be(member2);
        member1.Should().NotBe(member3);
    }
}

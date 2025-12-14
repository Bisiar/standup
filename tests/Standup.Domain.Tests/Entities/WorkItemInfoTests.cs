using FluentAssertions;
using Standup.Domain.Entities;
using Standup.Domain.Enums;
using Xunit;

namespace Standup.Domain.Tests.Entities;

public class WorkItemInfoTests
{
    [Fact]
    public void WorkItemInfo_CanBeCreatedWithRequiredProperties()
    {
        // Act
        var workItem = new WorkItemInfo(
            Id: "123",
            Title: "Implement feature X",
            Type: "User Story",
            Status: WorkItemStatus.InProgress,
            SourceType: SourceType.AzureDevOps,
            Url: "https://dev.azure.com/org/project/_workitems/edit/123");

        // Assert
        workItem.Id.Should().Be("123");
        workItem.Title.Should().Be("Implement feature X");
        workItem.Type.Should().Be("User Story");
        workItem.Status.Should().Be(WorkItemStatus.InProgress);
        workItem.SourceType.Should().Be(SourceType.AzureDevOps);
        workItem.Url.Should().Be("https://dev.azure.com/org/project/_workitems/edit/123");
        workItem.AssignedTo.Should().BeNull();
        workItem.ParentId.Should().BeNull();
        workItem.Tags.Should().NotBeNull();
        workItem.Tags.Should().BeEmpty();
    }

    [Fact]
    public void WorkItemInfo_SupportsOptionalProperties()
    {
        // Arrange
        var tags = new List<string> { "backend", "critical" };

        // Act
        var workItem = new WorkItemInfo(
            Id: "456",
            Title: "Fix bug Y",
            Type: "Bug",
            Status: WorkItemStatus.Resolved,
            SourceType: SourceType.GitHub,
            Url: "https://github.com/org/repo/issues/456",
            AssignedTo: "john.doe@example.com",
            ParentId: "123",
            Tags: tags);

        // Assert
        workItem.AssignedTo.Should().Be("john.doe@example.com");
        workItem.ParentId.Should().Be("123");
        workItem.Tags.Should().HaveCount(2);
        workItem.Tags.Should().Contain("backend");
        workItem.Tags.Should().Contain("critical");
    }

    [Fact]
    public void WorkItemInfo_WithNullTags_InitializesEmptyList()
    {
        // Act
        var workItem = new WorkItemInfo(
            Id: "789",
            Title: "Task X",
            Type: "Task",
            Status: WorkItemStatus.New,
            SourceType: SourceType.AzureDevOps,
            Url: "https://dev.azure.com/org/project/_workitems/edit/789",
            Tags: null);

        // Assert
        workItem.Tags.Should().NotBeNull();
        workItem.Tags.Should().BeEmpty();
    }

    [Fact]
    public void WorkItemInfo_IsRecord_SupportsEquality()
    {
        // Arrange
        var tags = new List<string> { "tag1" };
        var workItem1 = new WorkItemInfo(
            Id: "123",
            Title: "Task",
            Type: "Task",
            Status: WorkItemStatus.New,
            SourceType: SourceType.GitHub,
            Url: "https://github.com/org/repo/issues/123",
            Tags: tags);

        var workItem2 = new WorkItemInfo(
            Id: "123",
            Title: "Task",
            Type: "Task",
            Status: WorkItemStatus.New,
            SourceType: SourceType.GitHub,
            Url: "https://github.com/org/repo/issues/123",
            Tags: tags);

        // Assert
        workItem1.Should().Be(workItem2);
        (workItem1 == workItem2).Should().BeTrue();
    }

    [Fact]
    public void WorkItemInfo_DifferentValues_AreNotEqual()
    {
        // Arrange
        var workItem1 = new WorkItemInfo(
            Id: "123",
            Title: "Task A",
            Type: "Task",
            Status: WorkItemStatus.New,
            SourceType: SourceType.GitHub,
            Url: "https://github.com/org/repo/issues/123");

        var workItem2 = new WorkItemInfo(
            Id: "456",
            Title: "Task B",
            Type: "Task",
            Status: WorkItemStatus.New,
            SourceType: SourceType.GitHub,
            Url: "https://github.com/org/repo/issues/456");

        // Assert
        workItem1.Should().NotBe(workItem2);
        (workItem1 != workItem2).Should().BeTrue();
    }

    [Fact]
    public void WorkItemInfo_SupportsDeconstruction()
    {
        // Arrange
        var tags = new List<string> { "frontend", "ui" };
        var workItem = new WorkItemInfo(
            Id: "999",
            Title: "Update UI",
            Type: "Feature",
            Status: WorkItemStatus.InProgress,
            SourceType: SourceType.AzureDevOps,
            Url: "https://dev.azure.com/org/project/_workitems/edit/999",
            AssignedTo: "jane.smith@example.com",
            ParentId: "100",
            Tags: tags);

        // Act
        var (id, title, type, status, sourceType, url, assignedTo, parentId, extractedTags) = workItem;

        // Assert
        id.Should().Be("999");
        title.Should().Be("Update UI");
        type.Should().Be("Feature");
        status.Should().Be(WorkItemStatus.InProgress);
        sourceType.Should().Be(SourceType.AzureDevOps);
        url.Should().Be("https://dev.azure.com/org/project/_workitems/edit/999");
        assignedTo.Should().Be("jane.smith@example.com");
        parentId.Should().Be("100");
        extractedTags.Should().HaveCount(2);
    }

    [Theory]
    [InlineData(WorkItemStatus.New)]
    [InlineData(WorkItemStatus.Active)]
    [InlineData(WorkItemStatus.InProgress)]
    [InlineData(WorkItemStatus.Resolved)]
    [InlineData(WorkItemStatus.Closed)]
    public void WorkItemInfo_SupportsDifferentStatuses(WorkItemStatus status)
    {
        // Act
        var workItem = new WorkItemInfo(
            Id: "1",
            Title: "Test Item",
            Type: "Task",
            Status: status,
            SourceType: SourceType.GitHub,
            Url: "https://github.com/org/repo/issues/1");

        // Assert
        workItem.Status.Should().Be(status);
    }

    [Theory]
    [InlineData("User Story")]
    [InlineData("Bug")]
    [InlineData("Task")]
    [InlineData("Feature")]
    [InlineData("Epic")]
    [InlineData("Issue")]
    public void WorkItemInfo_SupportsDifferentTypes(string type)
    {
        // Act
        var workItem = new WorkItemInfo(
            Id: "1",
            Title: "Test Item",
            Type: type,
            Status: WorkItemStatus.New,
            SourceType: SourceType.GitHub,
            Url: "https://github.com/org/repo/issues/1");

        // Assert
        workItem.Type.Should().Be(type);
    }

    [Theory]
    [InlineData(SourceType.GitHub)]
    [InlineData(SourceType.AzureDevOps)]
    public void WorkItemInfo_SupportsDifferentSourceTypes(SourceType sourceType)
    {
        // Act
        var workItem = new WorkItemInfo(
            Id: "1",
            Title: "Test Item",
            Type: "Task",
            Status: WorkItemStatus.New,
            SourceType: sourceType,
            Url: "https://example.com/1");

        // Assert
        workItem.SourceType.Should().Be(sourceType);
    }

    [Fact]
    public void WorkItemInfo_CanHaveMultipleTags()
    {
        // Arrange
        var tags = new List<string> { "priority-high", "customer-facing", "security", "performance" };

        // Act
        var workItem = new WorkItemInfo(
            Id: "1",
            Title: "Critical Security Fix",
            Type: "Bug",
            Status: WorkItemStatus.InProgress,
            SourceType: SourceType.AzureDevOps,
            Url: "https://dev.azure.com/org/project/_workitems/edit/1",
            Tags: tags);

        // Assert
        workItem.Tags.Should().HaveCount(4);
        workItem.Tags.Should().ContainInOrder("priority-high", "customer-facing", "security", "performance");
    }

    [Fact]
    public void WorkItemInfo_WithParent_ReferencesParentId()
    {
        // Act
        var childItem = new WorkItemInfo(
            Id: "201",
            Title: "Implement login UI",
            Type: "Task",
            Status: WorkItemStatus.InProgress,
            SourceType: SourceType.AzureDevOps,
            Url: "https://dev.azure.com/org/project/_workitems/edit/201",
            ParentId: "200");

        // Assert
        childItem.ParentId.Should().Be("200");
    }

    [Fact]
    public void WorkItemInfo_WithAssignment_StoresAssignee()
    {
        // Act
        var workItem = new WorkItemInfo(
            Id: "1",
            Title: "Assigned Task",
            Type: "Task",
            Status: WorkItemStatus.InProgress,
            SourceType: SourceType.GitHub,
            Url: "https://github.com/org/repo/issues/1",
            AssignedTo: "developer@example.com");

        // Assert
        workItem.AssignedTo.Should().Be("developer@example.com");
    }
}

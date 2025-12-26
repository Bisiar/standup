using FluentAssertions;
using Standup.Application.Models;
using Standup.Application.ViewModels;
using Standup.Domain.Enums;
using Xunit;

namespace Standup.Application.Tests.ViewModels;

public class SelectableProjectTests
{
    [Fact]
    public void Constructor_InitializesProperties()
    {
        // Arrange
        var project = CreateTestProject();
        var callbackInvoked = false;
        Task Callback(SelectableProject p)
        {
            callbackInvoked = true;
            return Task.CompletedTask;
        }

        // Act
        var selectable = new SelectableProject(project, true, "CLIENT001", Callback);

        // Assert
        selectable.Id.Should().Be(project.Id);
        selectable.Name.Should().Be(project.Name);
        selectable.IsSelected.Should().BeTrue();
        selectable.ClientCode.Should().Be("CLIENT001");
        selectable.Project.Should().Be(project);
        callbackInvoked.Should().BeFalse(); // Callback not invoked during construction
    }

    [Fact]
    public async Task ChangingIsSelected_InvokesCallback()
    {
        // Arrange
        var project = CreateTestProject();
        SelectableProject? callbackArg = null;
        Task Callback(SelectableProject p)
        {
            callbackArg = p;
            return Task.CompletedTask;
        }

        var selectable = new SelectableProject(project, false, "CLIENT001", Callback);

        // Act
        selectable.IsSelected = true;
        await Task.Delay(10); // Allow async callback to complete

        // Assert
        callbackArg.Should().Be(selectable);
        selectable.IsSelected.Should().BeTrue();
    }

    [Fact]
    public void DisplayName_ForGitHub_ShowsOrgAndRepo()
    {
        // Arrange
        var project = new ProjectInstance(
            Id: "proj-1",
            Name: "Test Project",
            TenantName: "tenant1",
            ApiEndpoint: "https://api.github.com",
            SourceType: SourceType.GitHub,
            SourceOrganization: "octocat",
            SourceProject: null,
            SourceRepository: "hello-world");
        var selectable = new SelectableProject(project, false, "CLIENT001", _ => Task.CompletedTask);

        // Act
        var displayName = selectable.DisplayName;

        // Assert
        displayName.Should().Be("octocat/hello-world");
    }

    [Fact]
    public void DisplayName_ForAzureDevOps_ShowsOrgAndProject()
    {
        // Arrange
        var project = new ProjectInstance(
            Id: "proj-1",
            Name: "Test Project",
            TenantName: "tenant1",
            ApiEndpoint: "https://dev.azure.com/org",
            SourceType: SourceType.AzureDevOps,
            SourceOrganization: "contoso",
            SourceProject: "WebApp",
            SourceRepository: "frontend");
        var selectable = new SelectableProject(project, false, "CLIENT001", _ => Task.CompletedTask);

        // Act
        var displayName = selectable.DisplayName;

        // Assert
        displayName.Should().Be("contoso/WebApp");
    }

    [Fact]
    public void SourceTypeDisplay_ReturnsSourceTypeName()
    {
        // Arrange
        var project = CreateTestProject(SourceType.GitHub);
        var selectable = new SelectableProject(project, false, "CLIENT001", _ => Task.CompletedTask);

        // Act
        var sourceTypeDisplay = selectable.SourceTypeDisplay;

        // Assert
        sourceTypeDisplay.Should().Be("GitHub");
    }

    [Fact]
    public void ClientCode_CanBeChanged()
    {
        // Arrange
        var project = CreateTestProject();
        var selectable = new SelectableProject(project, false, "INITIAL", _ => Task.CompletedTask);

        // Act
        selectable.ClientCode = "CHANGED";

        // Assert
        selectable.ClientCode.Should().Be("CHANGED");
    }

    [Fact]
    public void IsSelected_DefaultsToFalse_WhenConstructedWithFalse()
    {
        // Arrange
        var project = CreateTestProject();

        // Act
        var selectable = new SelectableProject(project, false, "CLIENT001", _ => Task.CompletedTask);

        // Assert
        selectable.IsSelected.Should().BeFalse();
    }

    [Fact]
    public void Properties_ExposeUnderlyingProjectData()
    {
        // Arrange
        var project = CreateTestProject();
        var selectable = new SelectableProject(project, false, "CLIENT001", _ => Task.CompletedTask);

        // Assert
        selectable.Id.Should().Be(project.Id);
        selectable.Name.Should().Be(project.Name);
        selectable.Project.Should().Be(project);
    }

    private static ProjectInstance CreateTestProject(SourceType sourceType = SourceType.AzureDevOps)
    {
        return new ProjectInstance(
            Id: "test-project-1",
            Name: "Test Project",
            TenantName: "Test Tenant",
            ApiEndpoint: "https://dev.azure.com/test",
            SourceType: sourceType,
            SourceOrganization: "test-org",
            SourceProject: "test-project",
            SourceRepository: "test-repo");
    }
}

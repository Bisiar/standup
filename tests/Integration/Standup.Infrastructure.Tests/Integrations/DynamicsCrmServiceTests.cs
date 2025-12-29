using FluentAssertions;
using Microsoft.Extensions.Options;
using Standup.Common.Tests.Configuration;
using Standup.Domain.Interfaces;
using Standup.Infrastructure.Configuration;
using Standup.Infrastructure.Integrations;
using Xunit;

namespace Standup.Infrastructure.Tests.Integrations;

/// <summary>
/// Integration tests for DynamicsCrmService.
/// Tests connection to Dynamics 365 CRM and data retrieval for project dashboard.
/// Requires environment variables: CRM_INSTANCE_URL, CRM_TENANT_ID, CRM_CLIENT_ID, CRM_CLIENT_SECRET.
/// </summary>
public class DynamicsCrmServiceTests : IDisposable
{
    private readonly ICrmProjectService _service;
    private readonly DynamicsCrmOptions _options;
    private readonly bool _isConfigured;
    private readonly HttpClient _httpClient;

    public DynamicsCrmServiceTests()
    {
        // Load environment variables from .env file
        TestEnvironmentLoader.LoadEnvironmentVariables();

        _options = new DynamicsCrmOptions
        {
            InstanceUrl = Environment.GetEnvironmentVariable("CRM_INSTANCE_URL") ?? string.Empty,
            TenantId = Environment.GetEnvironmentVariable("CRM_TENANT_ID") ?? string.Empty,
            ClientId = Environment.GetEnvironmentVariable("CRM_CLIENT_ID") ?? string.Empty,
            ClientSecret = Environment.GetEnvironmentVariable("CRM_CLIENT_SECRET") ?? string.Empty,
            Enabled = true,
        };

        _isConfigured = !string.IsNullOrEmpty(_options.InstanceUrl) &&
                        !string.IsNullOrEmpty(_options.TenantId) &&
                        !string.IsNullOrEmpty(_options.ClientId) &&
                        !string.IsNullOrEmpty(_options.ClientSecret);

        _httpClient = new HttpClient();
        _service = new DynamicsCrmService(Options.Create(_options), _httpClient);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Validates that the CRM connection works.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ValidateConnection_WithValidCredentials_ReturnsTrue()
    {
        if (!_isConfigured)
        {
            Console.WriteLine("SKIPPED: CRM environment variables not configured");
            Console.WriteLine("Set: CRM_INSTANCE_URL, CRM_TENANT_ID, CRM_CLIENT_ID, CRM_CLIENT_SECRET");
            return;
        }

        // Act
        var isValid = await _service.ValidateConnectionAsync();

        // Assert
        Console.WriteLine($"CRM Connection Valid: {isValid}");
        Console.WriteLine($"Instance URL: {_options.InstanceUrl}");

        isValid.Should().BeTrue("CRM connection should be valid with configured credentials");
    }

    /// <summary>
    /// Gets all projects from CRM to discover available data.
    /// This test documents what projects exist and what data is available for the dashboard.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetAllProjects_ReturnsProjects_WithDashboardData()
    {
        if (!_isConfigured)
        {
            Console.WriteLine("SKIPPED: CRM not configured");
            return;
        }

        // Act
        var projects = await _service.GetAllProjectsAsync();

        // Assert
        Console.WriteLine($"=== CRM PROJECTS ({projects.Count} total) ===");
        Console.WriteLine();

        foreach (var project in projects.Take(20))
        {
            Console.WriteLine($"Project: {project.ProjectName}");
            Console.WriteLine($"  ID: {project.CrmProjectId}");
            Console.WriteLine($"  Client Code: {project.ClientCode}");
            Console.WriteLine($"  Status: {project.Status}");
            Console.WriteLine($"  Start Date: {project.StartDate:yyyy-MM-dd}");
            Console.WriteLine($"  End Date: {project.EndDate:yyyy-MM-dd}");
            Console.WriteLine($"  Progress: {project.PercentComplete}%");
            Console.WriteLine($"  Project Manager: {project.ProjectManager}");
            Console.WriteLine($"  Budget Hours: {project.BudgetHours}");
            Console.WriteLine($"  Hours Used: {project.HoursUsed}");
            Console.WriteLine();
        }

        projects.Should().NotBeEmpty("CRM should have projects");

        // Document data mapping for dashboard
        Console.WriteLine("=== DASHBOARD DATA MAPPING ===");
        Console.WriteLine("CrmProject.ProjectName      → Dashboard Header");
        Console.WriteLine("CrmProject.StartDate        → Start Date");
        Console.WriteLine("CrmProject.EndDate          → Target Date");
        Console.WriteLine("CrmProject.PercentComplete  → Overall Progress %");
        Console.WriteLine("CrmProject.BudgetHours      → Budget (if available)");
        Console.WriteLine("CrmProject.HoursUsed        → Budget Used");
        Console.WriteLine("CrmProject.Status           → Health Status");
    }

    /// <summary>
    /// Gets a specific project by name (UPHT) to see detailed data for dashboard.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetProjectByName_UPHT_ReturnsDashboardData()
    {
        if (!_isConfigured)
        {
            Console.WriteLine("SKIPPED: CRM not configured");
            return;
        }

        // Act - Get all projects and find UPHT
        var projects = await _service.GetAllProjectsAsync();
        var uphtProject = projects.FirstOrDefault(p =>
            p.ProjectName.Contains("UPHT", StringComparison.OrdinalIgnoreCase) ||
            p.ClientCode.Contains("UPHT", StringComparison.OrdinalIgnoreCase));

        // Assert
        Console.WriteLine("=== UPHT PROJECT DATA ===");

        if (uphtProject != null)
        {
            Console.WriteLine($"Found: {uphtProject.ProjectName}");
            Console.WriteLine($"  ID: {uphtProject.CrmProjectId}");
            Console.WriteLine($"  Start: {uphtProject.StartDate:yyyy-MM-dd}");
            Console.WriteLine($"  End: {uphtProject.EndDate:yyyy-MM-dd}");
            Console.WriteLine($"  Progress: {uphtProject.PercentComplete}%");
            Console.WriteLine($"  Status: {uphtProject.Status}");

            // Get milestones for this project
            if (!string.IsNullOrEmpty(uphtProject.CrmProjectId))
            {
                var milestones = await _service.GetUpcomingMilestonesAsync(uphtProject.CrmProjectId);

                Console.WriteLine();
                Console.WriteLine($"=== UPHT MILESTONES ({milestones.Count} total) ===");

                foreach (var milestone in milestones.Take(10))
                {
                    Console.WriteLine($"  {milestone.Name}");
                    Console.WriteLine($"    Due: {milestone.DueDate:yyyy-MM-dd}");
                    Console.WriteLine($"    Progress: {milestone.PercentComplete}%");
                    Console.WriteLine($"    Status: {milestone.Status}");
                    Console.WriteLine($"    Effort Estimated: {milestone.EffortEstimated}");
                    Console.WriteLine($"    Effort Completed: {milestone.EffortCompleted}");
                    Console.WriteLine($"    Effort Remaining: {milestone.EffortRemaining}");
                    Console.WriteLine();
                }

                Console.WriteLine("=== MILESTONE → PHASE MAPPING ===");
                Console.WriteLine("CrmMilestone.Name           → Phase Name");
                Console.WriteLine("CrmMilestone.DueDate        → Phase End Date");
                Console.WriteLine("CrmMilestone.PercentComplete → Phase Progress");
                Console.WriteLine("CrmMilestone.Status         → Complete/InProgress/Pending");
            }
        }
        else
        {
            Console.WriteLine("UPHT project not found. Available projects:");
            foreach (var p in projects.Take(10))
            {
                Console.WriteLine($"  - {p.ProjectName} ({p.ClientCode})");
            }
        }
    }

    /// <summary>
    /// Gets milestones for a project to verify phase data is available.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetUpcomingMilestones_ForAnyProject_ReturnsMilestones()
    {
        if (!_isConfigured)
        {
            Console.WriteLine("SKIPPED: CRM not configured");
            return;
        }

        // First get a project with an ID
        var projects = await _service.GetAllProjectsAsync();
        var projectWithId = projects.FirstOrDefault(p => !string.IsNullOrEmpty(p.CrmProjectId));

        if (projectWithId == null)
        {
            Console.WriteLine("No projects with valid IDs found");
            return;
        }

        Console.WriteLine($"Testing milestones for project: {projectWithId.ProjectName}");

        // Act
        var milestones = await _service.GetUpcomingMilestonesAsync(projectWithId.CrmProjectId);

        // Assert
        Console.WriteLine($"=== MILESTONES ({milestones.Count} total) ===");

        foreach (var milestone in milestones)
        {
            Console.WriteLine($"  {milestone.Name}");
            Console.WriteLine($"    ID: {milestone.MilestoneId}");
            Console.WriteLine($"    Due: {milestone.DueDate:yyyy-MM-dd}");
            Console.WriteLine($"    Progress: {milestone.PercentComplete}%");
            Console.WriteLine($"    Status: {milestone.Status}");
            Console.WriteLine($"    Description: {milestone.Description}");
            Console.WriteLine($"    Effort Estimated: {milestone.EffortEstimated}");
            Console.WriteLine($"    Effort Completed: {milestone.EffortCompleted}");
            Console.WriteLine($"    Effort Remaining: {milestone.EffortRemaining}");
            Console.WriteLine();
        }

        // These milestones should become the Project Timeline/Phases in the dashboard
        Console.WriteLine("=== NOTE ===");
        Console.WriteLine("These milestones should populate the 'Project Timeline' section in the dashboard.");
        Console.WriteLine("Map CrmMilestone → ProjectPhase for display.");
    }
}

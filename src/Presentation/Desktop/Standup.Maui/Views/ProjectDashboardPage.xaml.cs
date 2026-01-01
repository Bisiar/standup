// <copyright file="ProjectDashboardPage.xaml.cs" company="Standup">
// Copyright (c) Standup. All rights reserved.
// </copyright>

using Microsoft.Extensions.Options;
using Serilog;
using Standup.Application.Models;
using Standup.Application.ViewModels;
using Standup.Domain.Entities;
using Standup.Infrastructure.Configuration;
using Standup.Infrastructure.Integrations;

namespace Standup.Maui.Views;

/// <summary>
/// Project Dashboard page showing detailed project metrics, timeline, and status.
/// </summary>
public partial class ProjectDashboardPage : ContentPage
{
    private readonly ProjectDashboardViewModel _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectDashboardPage"/> class.
    /// </summary>
    /// <param name="viewModel">The view model.</param>
    public ProjectDashboardPage(ProjectDashboardViewModel viewModel)
    {
        Log.Information("ProjectDashboardPage constructor starting");

        _viewModel = viewModel;
        _viewModel.OnLoadCrmDataFromTenant += OnLoadCrmDataFromTenantAsync;
        BindingContext = _viewModel;

        InitializeComponent();

        Log.Information("ProjectDashboardPage InitializeComponent completed");
    }

    /// <summary>
    /// Loads the specified project into the dashboard.
    /// </summary>
    /// <param name="projectInstance">The project instance to display.</param>
    public void LoadProject(ProjectInstance projectInstance)
    {
        _viewModel.LoadProject(projectInstance);
    }

    /// <summary>
    /// Handles loading CRM data from a specific tenant.
    /// Creates a temporary DynamicsCrmService configured for the tenant.
    /// </summary>
    /// <param name="tenant">The CRM tenant configuration.</param>
    /// <param name="crmProjectId">The CRM project ID to load.</param>
    /// <returns>The CRM data result containing project and milestones.</returns>
    private async Task<CrmDataResult> OnLoadCrmDataFromTenantAsync(CrmTenantConfig tenant, string crmProjectId)
    {
        try
        {
            // Create options for this tenant
            var options = Options.Create(new DynamicsCrmOptions
            {
                InstanceUrl = tenant.InstanceUrl,
                TenantId = tenant.TenantId ?? string.Empty,
                ClientId = tenant.ClientId ?? string.Empty,
                ClientSecret = tenant.ClientSecret ?? string.Empty,
                Enabled = true,
            });

            // Create a temporary CRM service for this tenant
            using var httpClient = new HttpClient();
            var crmService = new DynamicsCrmService(options, httpClient);

            // Fetch project, milestones, and tasks in parallel
            var projectTask = crmService.GetProjectByIdAsync(crmProjectId);
            var milestonesTask = crmService.GetUpcomingMilestonesAsync(crmProjectId);
            var tasksTask = crmService.GetInProgressTasksAsync(crmProjectId);

            await Task.WhenAll(projectTask, milestonesTask, tasksTask);

            var project = await projectTask;
            var milestones = await milestonesTask;
            var tasks = await tasksTask;

            Log.Information(
                "Loaded CRM data from tenant {TenantName}: Project={ProjectName}, Milestones={Count}, Tasks={TaskCount}",
                tenant.Name,
                project?.ProjectName ?? "null",
                milestones.Count,
                tasks.Count);

            return new CrmDataResult(project, milestones, tasks);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load CRM data from tenant {TenantName}: {Message}", tenant.Name, ex.Message);
            return new CrmDataResult(null, Array.Empty<CrmMilestone>(), Array.Empty<CrmTask>());
        }
    }

    /// <summary>
    /// Handles the back button click.
    /// </summary>
    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}

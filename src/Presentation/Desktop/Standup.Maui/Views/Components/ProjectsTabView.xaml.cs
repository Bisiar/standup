// <copyright file="ProjectsTabView.xaml.cs" company="Standup">
// Copyright (c) Standup. All rights reserved.
// </copyright>

using Microsoft.Extensions.Options;
using Serilog;
using Standup.Application.Models;
using Standup.Application.ViewModels;
using Standup.Domain.Entities;
using Standup.Infrastructure.Configuration;
using Standup.Infrastructure.Integrations;

namespace Standup.Maui.Views.Components;

/// <summary>
/// Projects tab view component.
/// </summary>
public partial class ProjectsTabView : ContentView
{
    private ProjectListViewModel? _currentViewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectsTabView"/> class.
    /// </summary>
    public ProjectsTabView()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        // Unsubscribe from previous view model
        if (_currentViewModel != null)
        {
            _currentViewModel.OnLoadCrmProjectsFromTenant -= OnLoadCrmProjectsFromTenantAsync;
        }

        // Subscribe to new view model
        if (BindingContext is ProjectListViewModel vm)
        {
            _currentViewModel = vm;
            vm.OnLoadCrmProjectsFromTenant += OnLoadCrmProjectsFromTenantAsync;
        }
        else
        {
            _currentViewModel = null;
        }
    }

    /// <summary>
    /// Handles loading CRM projects from a specific tenant.
    /// Creates a temporary DynamicsCrmService configured for the tenant.
    /// </summary>
    private async Task<List<CrmProject>> OnLoadCrmProjectsFromTenantAsync(CrmTenantConfig tenant)
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
                Enabled = true
            });

            // Create a temporary CRM service for this tenant
            using var httpClient = new HttpClient();
            var crmService = new DynamicsCrmService(options, httpClient);

            var projects = await crmService.GetAllProjectsAsync();
            Log.Information("Loaded {Count} CRM projects from tenant {TenantName}", projects.Count, tenant.Name);
            return projects;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load CRM projects from tenant {TenantName}: {Message}", tenant.Name, ex.Message);
            return new List<CrmProject>();
        }
    }

    /// <summary>
    /// Handles the Open Full Dashboard button click.
    /// </summary>
    private async void OnOpenDashboardClicked(object? sender, EventArgs e)
    {
        try
        {
            Log.Information("OnOpenDashboardClicked called");

            if (BindingContext is not ProjectListViewModel vm)
            {
                Log.Warning("BindingContext is not ProjectListViewModel: {Type}", BindingContext?.GetType().Name ?? "null");
                return;
            }

            if (vm.SelectedProject == null)
            {
                Log.Warning("No project selected");
                return;
            }

            Log.Information("Opening dashboard for project: {ProjectName}", vm.SelectedProject.Name);

            var page = MauiProgram.ServiceProvider?.GetService<ProjectDashboardPage>();
            if (page == null)
            {
                Log.Error("Failed to resolve ProjectDashboardPage from DI");
                return;
            }

            Log.Information("ProjectDashboardPage resolved, calling LoadProject");
            page.LoadProject(vm.SelectedProject);

            Log.Information("Calling Navigation.PushAsync");
            await Navigation.PushAsync(page);
            Log.Information("Dashboard page pushed to navigation stack");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to open dashboard: {Message}", ex.Message);
        }
    }
}

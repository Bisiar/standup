// <copyright file="ProjectsTabView.xaml.cs" company="Standup">
// Copyright (c) Standup. All rights reserved.
// </copyright>

using Serilog;
using Standup.Application.ViewModels;

namespace Standup.Maui.Views.Components;

/// <summary>
/// Projects tab view component.
/// </summary>
public partial class ProjectsTabView : ContentView
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectsTabView"/> class.
    /// </summary>
    public ProjectsTabView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handles the Open Full Dashboard button click.
    /// </summary>
    private async void OnOpenDashboardClicked(object? sender, EventArgs e)
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

        page.LoadProject(vm.SelectedProject);
        await Navigation.PushAsync(page);
        Log.Information("Dashboard page pushed to navigation stack");
    }
}

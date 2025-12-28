// <copyright file="ProjectDashboardPage.xaml.cs" company="Standup">
// Copyright (c) Standup. All rights reserved.
// </copyright>

using Serilog;
using Standup.Application.Models;
using Standup.Application.ViewModels;

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
    /// Handles the back button click.
    /// </summary>
    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}

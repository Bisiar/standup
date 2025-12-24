using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using Standup.Application.DTOs;
using Standup.Application.Interfaces;
using Standup.Application.Models;
using Standup.Domain.Entities;

namespace Standup.Application.ViewModels;

/// <summary>
/// Main framework ViewModel that coordinates tab navigation and child ViewModels.
/// This ViewModel manages the application's tabbed interface state and cross-tab communication.
/// </summary>
public partial class FrameworkViewModel : ObservableObject
{
    private readonly IFolderPickerService _folderPickerService;

    [ObservableProperty]
    private int _selectedTabIndex = -1;

    [ObservableProperty]
    private bool _isInitialized;

    [ObservableProperty]
    private string _selectedTabTitle = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the Report tab should be visible (true after a report is generated).
    /// </summary>
    [ObservableProperty]
    private bool _showReportTab;

    // Child ViewModels for each tab
    public StandupViewModel StandupVm { get; }
    public GroupListViewModel GroupsVm { get; }
    public ProjectListViewModel ProjectsVm { get; }
    public DashboardViewModel DashboardVm { get; }
    public SettingsViewModel SettingsVm { get; }
    public ReportViewModel ReportVm { get; }

    public FrameworkViewModel(
        StandupViewModel standupVm,
        GroupListViewModel groupsVm,
        ProjectListViewModel projectsVm,
        DashboardViewModel dashboardVm,
        SettingsViewModel settingsVm,
        ReportViewModel reportVm,
        IFolderPickerService folderPickerService,
        IReportCacheService? cacheService = null)
    {
        StandupVm = standupVm;
        GroupsVm = groupsVm;
        ProjectsVm = projectsVm;
        DashboardVm = dashboardVm;
        SettingsVm = settingsVm;
        ReportVm = reportVm;
        _folderPickerService = folderPickerService;

        // Wire up the folder picker for GroupListViewModel
        GroupsVm.OnBrowseForFolder += BrowseForFolderAsync;

        // Wire up project report events
        ProjectsVm.OnProjectReportGenerated += HandleProjectReportGeneratedAsync;
        ProjectsVm.OnViewProjectReport += HandleViewProjectReportAsync;

        // Provide cache service to settings for cache management UI
        SettingsVm.SetCacheService(cacheService);

        Log.Information("FrameworkViewModel initialized with all child ViewModels");
    }

    /// <summary>
    /// Shows the Report tab with the generated report data.
    /// Called programmatically after report generation, not via command binding.
    /// </summary>
    /// <param name="report">The generated report data.</param>
    /// <param name="sourceGroup">The group used to generate the report.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ShowReportAsync(GroupedStandupReportDto report, RepositoryGroup sourceGroup)
    {
        Log.Information("ShowReportAsync called for group: {GroupName}", sourceGroup.Name);

        // Set the report data on the ReportViewModel
        ReportVm.SetReport(report, sourceGroup);

        // Also update DashboardViewModel with the report data
        DashboardVm.SetReport(report);

        // Show the Report tab and switch to it
        ShowReportTab = true;
        await SelectTabAsync(5);
    }

    /// <summary>
    /// Initialize the framework - call this from OnAppearing.
    /// </summary>
    [RelayCommand]
    private async Task InitializeAsync()
    {
        if (IsInitialized)
        {
            return;
        }

        Log.Information("FrameworkViewModel initializing");

        // Select the first tab (Standup) by default
        await SelectTabAsync(0);

        IsInitialized = true;
        Log.Information("FrameworkViewModel initialization complete");
    }

    /// <summary>
    /// Select a tab by index and trigger its load command.
    /// Tab indexes: 0=Standup, 1=Groups, 2=Projects, 3=Dashboard, 4=Settings, 5=Report (dynamic).
    /// </summary>
    [RelayCommand]
    private async Task SelectTabAsync(int index)
    {
        // Allow 0-4 always, 5 (Report) only if ShowReportTab is true
        if (index < 0 || index > 5)
        {
            return;
        }

        if (index == 5 && !ShowReportTab)
        {
            return;
        }

        SelectedTabIndex = index;
        SelectedTabTitle = index switch
        {
            0 => "Standup",
            1 => "Groups",
            2 => "Projects",
            3 => "Dashboard",
            4 => "Settings",
            5 => "Report",
            _ => string.Empty,
        };

        Log.Information("Tab selected: {Index} ({Title})", index, SelectedTabTitle);

        // Trigger load on the selected tab's ViewModel
        await LoadTabDataAsync(index);
    }

    private async Task LoadTabDataAsync(int tabIndex)
    {
        try
        {
            switch (tabIndex)
            {
                case 0:
                    await StandupVm.LoadCommand.ExecuteAsync(null);
                    break;
                case 1:
                    await GroupsVm.LoadGroupsCommand.ExecuteAsync(null);
                    break;
                case 2:
                    await ProjectsVm.LoadProjectsCommand.ExecuteAsync(null);
                    break;
                case 3:
                    // Dashboard auto-fetches from local repos if no report is loaded
                    await DashboardVm.LoadCommand.ExecuteAsync(null);
                    break;
                case 4:
                    await SettingsVm.LoadCommand.ExecuteAsync(null);
                    break;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error loading data for tab {Index}", tabIndex);
        }
    }

    /// <summary>
    /// Navigate to the Standup tab with a specific group selected.
    /// Used for cross-tab navigation (e.g., from Groups tab Generate button).
    /// </summary>
    [RelayCommand]
    private async Task NavigateToStandupWithGroupAsync(string groupId)
    {
        Log.Information("Navigating to Standup tab with group: {GroupId}", groupId);

        // First select the standup tab
        await SelectTabAsync(0);

        // Then select the group in StandupViewModel
        // Note: StandupViewModel needs a method to select a group by ID
        // This will be implemented when the StandupViewModel is updated
    }

    /// <summary>
    /// Folder picker implementation that delegates to IFolderPickerService.
    /// This is wired up to GroupListViewModel.OnBrowseForFolder.
    /// </summary>
    private async Task<string?> BrowseForFolderAsync()
    {
        Log.Information("BrowseForFolderAsync called via FrameworkViewModel");
        return await _folderPickerService.BrowseForRepositoryFolderAsync();
    }

    /// <summary>
    /// Hides the Report tab and clears report data.
    /// </summary>
    [RelayCommand]
    private void HideReport()
    {
        ShowReportTab = false;
        ReportVm.ClearReportCommand.Execute(null);

        // If currently on Report tab, go back to Standup
        if (SelectedTabIndex == 5)
        {
            _ = SelectTabAsync(0);
        }
    }

    /// <summary>
    /// Handles a newly generated report from a project.
    /// Creates a temporary group for display purposes and shows the report.
    /// </summary>
    private async Task HandleProjectReportGeneratedAsync(GroupedStandupReportDto report, ProjectInstance project)
    {
        Log.Information("Project report generated for: {ProjectName}", project.Name);

        // Create a temporary group representation for the ReportViewModel
        var tempGroup = new RepositoryGroup
        {
            Id = project.Id,
            Name = project.Name,
            Description = $"Report for project: {project.Name}",
        };

        // Set the report and show the tab
        ReportVm.SetReport(report, tempGroup);
        DashboardVm.SetReport(report);
        ShowReportTab = true;
        await SelectTabAsync(5);
    }

    /// <summary>
    /// Handles viewing a saved report from project history.
    /// </summary>
    private async Task HandleViewProjectReportAsync(ReportHistory history, ProjectInstance project)
    {
        Log.Information("Viewing saved report for project: {ProjectName}", project.Name);

        // Set the report from history (Dashboard won't have data for history views)
        ReportVm.SetReportFromHistory(history, project.Name);
        DashboardVm.SetReport(null);
        ShowReportTab = true;
        await SelectTabAsync(5);
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using Standup.Application.DTOs;
using Standup.Application.Interfaces;
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
    /// Whether the Report tab should be visible (true after a report is generated).
    /// </summary>
    [ObservableProperty]
    private bool _showReportTab;

    // Child ViewModels for each tab
    public StandupViewModel StandupVm { get; }
    public GroupListViewModel GroupsVm { get; }
    public ProjectListViewModel ProjectsVm { get; }
    public SettingsViewModel SettingsVm { get; }
    public ReportViewModel ReportVm { get; }

    public FrameworkViewModel(
        StandupViewModel standupVm,
        GroupListViewModel groupsVm,
        ProjectListViewModel projectsVm,
        SettingsViewModel settingsVm,
        ReportViewModel reportVm,
        IFolderPickerService folderPickerService)
    {
        StandupVm = standupVm;
        GroupsVm = groupsVm;
        ProjectsVm = projectsVm;
        SettingsVm = settingsVm;
        ReportVm = reportVm;
        _folderPickerService = folderPickerService;

        // Wire up the folder picker for GroupListViewModel
        GroupsVm.OnBrowseForFolder += BrowseForFolderAsync;

        Log.Information("FrameworkViewModel initialized with all child ViewModels");
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
    /// </summary>
    [RelayCommand]
    private async Task SelectTabAsync(int index)
    {
        // Allow 0-3 always, 4 (Report) only if ShowReportTab is true
        if (index < 0 || index > 4)
        {
            return;
        }

        if (index == 4 && !ShowReportTab)
        {
            return;
        }

        SelectedTabIndex = index;
        SelectedTabTitle = index switch
        {
            0 => "Standup",
            1 => "Groups",
            2 => "Projects",
            3 => "Settings",
            4 => "Report",
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
    /// Shows the Report tab with the generated report data.
    /// Called programmatically after report generation, not via command binding.
    /// </summary>
    /// <param name="report">The generated report data.</param>
    /// <param name="sourceGroup">The group used to generate the report.</param>
    public async Task ShowReportAsync(GroupedStandupReportDto report, RepositoryGroup sourceGroup)
    {
        Log.Information("ShowReportAsync called for group: {GroupName}", sourceGroup.Name);

        // Set the report data on the ReportViewModel
        ReportVm.SetReport(report, sourceGroup);

        // Show the Report tab and switch to it
        ShowReportTab = true;
        await SelectTabAsync(4);
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
        if (SelectedTabIndex == 4)
        {
            _ = SelectTabAsync(0);
        }
    }
}

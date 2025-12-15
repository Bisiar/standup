using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Standup.Application.DTOs;
using Standup.Application.Interfaces;
using Standup.Application.Models;
using Standup.Application.Services;
using Standup.Domain.Entities;
using Standup.Domain.Enums;

namespace Standup.Application.ViewModels;

public partial class ProjectListViewModel : ObservableObject
{
    private readonly IProjectService _projectService;
    private readonly ILocalStandupService _localStandupService;
    private readonly ReportHistoryService _reportHistoryService;

    /// <summary>
    /// Event raised when a report is generated for a project.
    /// The FrameworkViewModel subscribes to this to display the report.
    /// </summary>
    public event Func<GroupedStandupReportDto, ProjectInstance, Task>? OnProjectReportGenerated;

    /// <summary>
    /// Event raised when viewing a saved report for a project.
    /// </summary>
    public event Func<ReportHistory, ProjectInstance, Task>? OnViewProjectReport;

    [ObservableProperty]
    private ObservableCollection<ProjectInstance> _projects = new();

    [ObservableProperty]
    private ProjectInstance? _selectedProject;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isGeneratingReport;

    [ObservableProperty]
    private double _reportProgress;

    [ObservableProperty]
    private string _reportStatusMessage = string.Empty;

    [ObservableProperty]
    private bool _isAddingProject;

    [ObservableProperty]
    private string _newProjectName = string.Empty;

    [ObservableProperty]
    private string _newTenantName = string.Empty;

    [ObservableProperty]
    private string _newApiEndpoint = string.Empty;

    // Edit project properties
    [ObservableProperty]
    private bool _isEditingProject;

    [ObservableProperty]
    private ProjectInstance? _editingProject;

    [ObservableProperty]
    private string _editProjectName = string.Empty;

    [ObservableProperty]
    private string _editTenantName = string.Empty;

    [ObservableProperty]
    private string _editSourcePat = string.Empty;

    [ObservableProperty]
    private string _editAuthorIdentifier = string.Empty;

    public ProjectListViewModel(
        IProjectService projectService,
        ILocalStandupService localStandupService,
        ReportHistoryService reportHistoryService)
    {
        _projectService = projectService;
        _localStandupService = localStandupService;
        _reportHistoryService = reportHistoryService;
    }

    [RelayCommand]
    private async Task LoadProjectsAsync()
    {
        IsLoading = true;
        try
        {
            var projects = await _projectService.GetProjectsAsync();
            Projects.Clear();
            foreach (var project in projects)
            {
                Projects.Add(project);
            }

            SelectedProject = await _projectService.GetCurrentProjectAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SelectProjectAsync(ProjectInstance project)
    {
        await _projectService.SetCurrentProjectAsync(project.Id);
        SelectedProject = project;
    }

    [RelayCommand]
    private void ShowAddProject()
    {
        IsAddingProject = true;
        NewProjectName = string.Empty;
        NewTenantName = string.Empty;
        NewApiEndpoint = string.Empty;
    }

    [RelayCommand]
    private async Task AddProjectAsync()
    {
        if (string.IsNullOrWhiteSpace(NewProjectName) || string.IsNullOrWhiteSpace(NewApiEndpoint))
        {
            return;
        }

        var project = new ProjectInstance(
            Id: Guid.NewGuid().ToString(),
            Name: NewProjectName,
            TenantName: NewTenantName,
            ApiEndpoint: NewApiEndpoint);

        var added = await _projectService.AddProjectAsync(project);
        Projects.Add(added);
        IsAddingProject = false;
    }

    [RelayCommand]
    private void CancelAddProject()
    {
        IsAddingProject = false;
    }

    [RelayCommand]
    private async Task DeleteProjectAsync(ProjectInstance project)
    {
        await _projectService.DeleteProjectAsync(project.Id);
        Projects.Remove(project);
    }

    [RelayCommand]
    private void EditProject(ProjectInstance project)
    {
        EditingProject = project;
        EditProjectName = project.Name;
        EditTenantName = project.TenantName;
        EditSourcePat = project.SourcePat ?? string.Empty;
        EditAuthorIdentifier = project.AuthorIdentifier ?? string.Empty;
        IsEditingProject = true;
    }

    [RelayCommand]
    private async Task SaveEditProjectAsync()
    {
        if (EditingProject == null || string.IsNullOrWhiteSpace(EditProjectName))
        {
            return;
        }

        // Create updated project with new values
        var updatedProject = EditingProject with
        {
            Name = EditProjectName,
            TenantName = EditTenantName,
            SourcePat = string.IsNullOrWhiteSpace(EditSourcePat) ? null : EditSourcePat,
            AuthorIdentifier = string.IsNullOrWhiteSpace(EditAuthorIdentifier) ? null : EditAuthorIdentifier
        };

        await _projectService.UpdateProjectAsync(updatedProject);

        // Update in local list
        var index = Projects.IndexOf(EditingProject);
        if (index >= 0)
        {
            Projects[index] = updatedProject;
        }

        IsEditingProject = false;
        EditingProject = null;
    }

    [RelayCommand]
    private void CancelEditProject()
    {
        IsEditingProject = false;
        EditingProject = null;
        EditProjectName = string.Empty;
        EditTenantName = string.Empty;
        EditSourcePat = string.Empty;
        EditAuthorIdentifier = string.Empty;
    }

    /// <summary>
    /// Generates a standup report for a single project.
    /// Creates a temporary RepositoryGroup with the project's repository configuration
    /// and generates a grouped report that can be displayed in the standard report UI.
    /// The report is saved to history using the project ID for later retrieval.
    /// </summary>
    [RelayCommand]
    private async Task RunReportAsync(ProjectInstance project)
    {
        if (string.IsNullOrEmpty(project.SourceOrganization) ||
            string.IsNullOrEmpty(project.SourceProject) ||
            string.IsNullOrEmpty(project.SourceRepository))
        {
            ReportStatusMessage = "Project source configuration incomplete. Please edit the project.";
            return;
        }

        if (string.IsNullOrEmpty(project.SourcePat))
        {
            ReportStatusMessage = "PAT not configured. Please edit the project to add a PAT.";
            return;
        }

        IsGeneratingReport = true;
        ReportProgress = 0;
        ReportStatusMessage = $"Generating report for {project.Name}...";

        try
        {
            // Create a temporary RepositoryGroup with the single project's repository
            // Use project.Id as the group ID so we can retrieve reports by project later
            var tempRepo = new GroupedRepository
            {
                Id = Guid.NewGuid().ToString(),
                ClientCode = project.TenantName ?? project.Name,
                SourceType = project.SourceType,
                Organization = project.SourceOrganization,
                Project = project.SourceProject,
                Repository = project.SourceRepository,
                EncryptedPat = project.SourcePat, // Note: PAT is stored unencrypted in ProjectInstance
                AuthorIdentifier = project.AuthorIdentifier,
                IsActive = true,
            };

            var tempGroup = new RepositoryGroup
            {
                Id = project.Id, // Use project ID so we can retrieve reports later
                Name = project.Name,
                Description = $"Single project report for {project.Name}",
                Repositories = new List<GroupedRepository> { tempRepo },
            };

            // Progress handler
            var progress = new Progress<(double Progress, string Message)>(update =>
            {
                ReportProgress = update.Progress;
                ReportStatusMessage = update.Message;
            });

            // Generate the report for last 7 days
            var since = DateTimeOffset.UtcNow.AddDays(-7);
            var until = DateTimeOffset.UtcNow;

            var report = await _localStandupService.GenerateGroupedStandupAsync(
                tempGroup,
                _ => Task.FromResult<string?>(project.SourcePat), // Return the PAT directly
                since,
                until,
                SummaryType.Executive,
                progress);

            // Update the report ID to use project ID for consistent storage
            report = report with { Id = project.Id };

            ReportProgress = 1.0;
            ReportStatusMessage = $"Report generated: {report.TotalCommits} commits";

            // Save the report to history
            var reportContent = StandupReportFormatter.BuildGroupedReportMarkdown(report);
            await _reportHistoryService.SaveReportAsync(report, reportContent);

            // Notify listeners (FrameworkViewModel) to display the report
            if (OnProjectReportGenerated != null)
            {
                await OnProjectReportGenerated.Invoke(report, project);
            }
        }
        catch (Exception ex)
        {
            ReportStatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsGeneratingReport = false;
        }
    }

    /// <summary>
    /// Views the latest saved report for a project.
    /// </summary>
    [RelayCommand]
    private async Task ViewLatestReportAsync(ProjectInstance project)
    {
        try
        {
            var latestReport = await _reportHistoryService.GetLatestByGroupIdAsync(project.Id);

            if (latestReport == null)
            {
                ReportStatusMessage = "No saved report found. Click refresh to generate one.";
                return;
            }

            // Notify listeners to display the saved report
            if (OnViewProjectReport != null)
            {
                await OnViewProjectReport.Invoke(latestReport, project);
            }
        }
        catch (Exception ex)
        {
            ReportStatusMessage = $"Error: {ex.Message}";
        }
    }
}

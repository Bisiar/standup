using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using Standup.Application.DTOs;
using Standup.Application.Interfaces;
using Standup.Application.Models;
using Standup.Application.Services;
using Standup.Domain.Entities;
using Standup.Domain.Enums;
using Standup.Domain.Interfaces;
using CrmProject = Standup.Domain.Entities.CrmProject;
using CrmTenantConfig = Standup.Application.Models.CrmTenantConfig;

namespace Standup.Application.ViewModels;

public partial class ProjectListViewModel : ObservableObject
{
    private readonly IProjectService _projectService;
    private readonly ILocalStandupService _localStandupService;
    private readonly ReportHistoryService _reportHistoryService;
    private readonly ISourceProviderFactory? _sourceProviderFactory;
    private readonly IEncryptionService? _encryptionService;
    private readonly ICrmProjectService? _crmProjectService;
    private readonly ICrmTenantConfigService? _crmTenantConfigService;

    /// <summary>
    /// Event raised when a report is generated for a project.
    /// The FrameworkViewModel subscribes to this to display the report.
    /// </summary>
    public event Func<GroupedStandupReportDto, ProjectInstance, Task>? OnProjectReportGenerated;

    /// <summary>
    /// Event raised when viewing a saved report for a project.
    /// </summary>
    public event Func<ReportHistory, ProjectInstance, Task>? OnViewProjectReport;

    /// <summary>
    /// Event raised when CRM projects should be loaded from a specific tenant.
    /// The MAUI layer handles this to create a CRM service for the tenant.
    /// Returns the list of CRM projects from that tenant.
    /// </summary>
    public event Func<CrmTenantConfig, Task<List<CrmProject>>>? OnLoadCrmProjectsFromTenant;

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
    [NotifyPropertyChangedFor(nameof(ShouldShowAddProjectButton))]
    private bool _isAddingProject;

    [ObservableProperty]
    private string _newProjectName = string.Empty;

    [ObservableProperty]
    private string _newTenantName = string.Empty;

    [ObservableProperty]
    private string _newApiEndpoint = string.Empty;

    [ObservableProperty]
    private SourceType _newSourceType = SourceType.AzureDevOps;

    [ObservableProperty]
    private string _newSourceOrganization = string.Empty;

    [ObservableProperty]
    private string _newSourceProject = string.Empty;

    [ObservableProperty]
    private string _newSourceRepository = string.Empty;

    [ObservableProperty]
    private string _newSourcePat = string.Empty;

    /// <summary>
    /// Gets the available source types for the picker.
    /// </summary>
    public IReadOnlyList<SourceType> AvailableSourceTypes { get; } = new[] { SourceType.AzureDevOps, SourceType.GitHub };

    // Edit project properties
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShouldShowAddProjectButton))]
    private bool _isEditingProject;

    /// <summary>
    /// Gets a value indicating whether the Add Project button should be visible.
    /// </summary>
    public bool ShouldShowAddProjectButton => !IsAddingProject && !IsEditingProject;

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

    [ObservableProperty]
    private string _editCrmProjectId = string.Empty;

    [ObservableProperty]
    private ObservableCollection<CrmTenantConfig> _availableCrmTenants = new();

    [ObservableProperty]
    private CrmTenantConfig? _selectedCrmTenant;

    [ObservableProperty]
    private ObservableCollection<CrmProject> _availableCrmProjects = new();

    [ObservableProperty]
    private CrmProject? _selectedCrmProject;

    [ObservableProperty]
    private bool _isLoadingCrmProjects;

    public ProjectListViewModel(
        IProjectService projectService,
        ILocalStandupService localStandupService,
        ReportHistoryService reportHistoryService,
        ISourceProviderFactory? sourceProviderFactory = null,
        IEncryptionService? encryptionService = null,
        ICrmProjectService? crmProjectService = null,
        ICrmTenantConfigService? crmTenantConfigService = null)
    {
        _projectService = projectService;
        _localStandupService = localStandupService;
        _reportHistoryService = reportHistoryService;
        _sourceProviderFactory = sourceProviderFactory;
        _encryptionService = encryptionService;
        _crmProjectService = crmProjectService;
        _crmTenantConfigService = crmTenantConfigService;
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

            // Notify computed properties to refresh after loading
            NotifyProjectCountsChanged();
            Log.Information("LoadProjectsAsync: Loaded {Count} projects, notified UI", Projects.Count);

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

        // Load stats for selected project (defined in partial class)
        await LoadProjectStatsAsync(project);
    }

    [RelayCommand]
    private void ShowAddProject()
    {
        IsAddingProject = true;
        NewProjectName = string.Empty;
        NewTenantName = string.Empty;
        NewApiEndpoint = string.Empty;
        NewSourceType = SourceType.AzureDevOps;
        NewSourceOrganization = string.Empty;
        NewSourceProject = string.Empty;
        NewSourceRepository = string.Empty;
        NewSourcePat = string.Empty;
    }

    [RelayCommand]
    private async Task AddProjectAsync()
    {
        // Require project name and source configuration
        if (string.IsNullOrWhiteSpace(NewProjectName) ||
            string.IsNullOrWhiteSpace(NewSourceOrganization) ||
            string.IsNullOrWhiteSpace(NewSourceRepository))
        {
            return;
        }

        // Azure DevOps requires Project, GitHub does not
        if (NewSourceType == SourceType.AzureDevOps && string.IsNullOrWhiteSpace(NewSourceProject))
        {
            return;
        }

        var project = new ProjectInstance(
            Id: Guid.NewGuid().ToString(),
            Name: NewProjectName,
            TenantName: NewTenantName,
            ApiEndpoint: NewApiEndpoint,
            SourceType: NewSourceType,
            SourceOrganization: NewSourceOrganization,
            SourceProject: NewSourceType == SourceType.AzureDevOps ? NewSourceProject : null,
            SourceRepository: NewSourceRepository,
            SourcePat: string.IsNullOrWhiteSpace(NewSourcePat) ? null : NewSourcePat);

        var added = await _projectService.AddProjectAsync(project);
        Projects.Add(added);
        NotifyProjectCountsChanged();
        IsAddingProject = false;
    }

    private void NotifyProjectCountsChanged()
    {
        OnPropertyChanged(nameof(GroupedProjects));
        OnPropertyChanged(nameof(TotalProjectCount));
        OnPropertyChanged(nameof(ConfiguredProjectCount));
        OnPropertyChanged(nameof(NeedsPatProjectCount));
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
        NotifyProjectCountsChanged();
    }

    [RelayCommand]
    private async Task EditProjectAsync(ProjectInstance project)
    {
        EditingProject = project;
        EditProjectName = project.Name;
        EditTenantName = project.TenantName;
        EditSourcePat = project.SourcePat ?? string.Empty;
        EditAuthorIdentifier = project.AuthorIdentifier ?? string.Empty;
        EditCrmProjectId = project.CrmProjectId ?? string.Empty;
        IsEditingProject = true;

        // Load available CRM tenants
        await LoadCrmTenantsAsync();

        // If project has a CRM tenant linked, select it and load its projects
        if (project.CrmTenantConfigId.HasValue)
        {
            SelectedCrmTenant = AvailableCrmTenants.FirstOrDefault(t => t.Id == project.CrmTenantConfigId.Value);
        }
        else if (AvailableCrmTenants.Count == 1)
        {
            // Auto-select if only one tenant configured
            SelectedCrmTenant = AvailableCrmTenants.First();
        }
    }

    /// <summary>
    /// Loads available CRM tenants for the dropdown.
    /// </summary>
    private async Task LoadCrmTenantsAsync()
    {
        if (_crmTenantConfigService == null)
        {
            Log.Debug("CRM tenant config service not available");
            return;
        }

        try
        {
            var tenants = await _crmTenantConfigService.GetAllAsync();
            AvailableCrmTenants.Clear();
            foreach (var tenant in tenants)
            {
                AvailableCrmTenants.Add(tenant);
            }

            Log.Information("Loaded {Count} CRM tenants for dropdown", tenants.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load CRM tenants");
        }
    }

    /// <summary>
    /// Called when SelectedCrmTenant changes. Loads CRM projects from the selected tenant.
    /// </summary>
    partial void OnSelectedCrmTenantChanged(CrmTenantConfig? value)
    {
        if (value != null)
        {
            _ = LoadCrmProjectsFromTenantAsync(value);
        }
        else
        {
            AvailableCrmProjects.Clear();
            SelectedCrmProject = null;
        }
    }

    /// <summary>
    /// Loads CRM projects from a specific tenant.
    /// </summary>
    private async Task LoadCrmProjectsFromTenantAsync(CrmTenantConfig tenant)
    {
        IsLoadingCrmProjects = true;
        AvailableCrmProjects.Clear();
        SelectedCrmProject = null;

        try
        {
            if (OnLoadCrmProjectsFromTenant != null)
            {
                var projects = await OnLoadCrmProjectsFromTenant.Invoke(tenant);
                foreach (var project in projects)
                {
                    AvailableCrmProjects.Add(project);
                }

                Log.Information("Loaded {Count} CRM projects from tenant {TenantName}", projects.Count, tenant.Name);

                // Select current project if it matches
                if (!string.IsNullOrEmpty(EditCrmProjectId))
                {
                    SelectedCrmProject = AvailableCrmProjects.FirstOrDefault(p => p.CrmProjectId == EditCrmProjectId);
                }
            }
            else
            {
                Log.Warning("OnLoadCrmProjectsFromTenant event not subscribed");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load CRM projects from tenant {TenantName}", tenant.Name);
        }
        finally
        {
            IsLoadingCrmProjects = false;
        }
    }

    [RelayCommand]
    private async Task LoadCrmProjectsAsync()
    {
        if (_crmProjectService == null)
        {
            Log.Debug("CRM service not available");
            return;
        }

        IsLoadingCrmProjects = true;
        try
        {
            var projects = await _crmProjectService.GetAllProjectsAsync();
            AvailableCrmProjects.Clear();
            foreach (var project in projects)
            {
                AvailableCrmProjects.Add(project);
            }

            Log.Information("Loaded {Count} CRM projects for dropdown", projects.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load CRM projects: {ErrorMessage}", ex.Message);
        }
        finally
        {
            IsLoadingCrmProjects = false;
        }
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
            AuthorIdentifier = string.IsNullOrWhiteSpace(EditAuthorIdentifier) ? null : EditAuthorIdentifier,
            CrmTenantConfigId = SelectedCrmTenant?.Id,
            CrmProjectId = SelectedCrmProject?.CrmProjectId,
            CrmProjectName = SelectedCrmProject?.ProjectName
        };

        await _projectService.UpdateProjectAsync(updatedProject);

        // Update in local list
        var index = Projects.IndexOf(EditingProject);
        if (index >= 0)
        {
            Projects[index] = updatedProject;
        }

        // Update SelectedProject so the detail view refreshes
        SelectedProject = updatedProject;

        IsEditingProject = false;
        EditingProject = null;
        SelectedCrmTenant = null;
        SelectedCrmProject = null;
        AvailableCrmTenants.Clear();
        AvailableCrmProjects.Clear();
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
        EditCrmProjectId = string.Empty;
        SelectedCrmTenant = null;
        SelectedCrmProject = null;
        AvailableCrmTenants.Clear();
        AvailableCrmProjects.Clear();
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
        // Validate required fields - SourceProject is only required for Azure DevOps
        var requiresProject = project.SourceType == SourceType.AzureDevOps;
        if (string.IsNullOrEmpty(project.SourceOrganization) ||
            string.IsNullOrEmpty(project.SourceRepository) ||
            (requiresProject && string.IsNullOrEmpty(project.SourceProject)))
        {
            ReportStatusMessage = "Project source configuration incomplete. Please edit the project.";
            Log.Warning(
                "Project configuration incomplete: Org={Org}, Project={Project}, Repo={Repo}, SourceType={SourceType}",
                project.SourceOrganization ?? "(null)",
                project.SourceProject ?? "(null)",
                project.SourceRepository ?? "(null)",
                project.SourceType);
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
                ClientCode = string.IsNullOrEmpty(project.TenantName) ? project.Name : project.TenantName,
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
    /// If no report exists, automatically generates one.
    /// </summary>
    [RelayCommand]
    private async Task ViewLatestReportAsync(ProjectInstance project)
    {
        Log.Information("ViewLatestReportAsync called for project: {ProjectName} (Id: {ProjectId})", project.Name, project.Id);

        try
        {
            var latestReport = await _reportHistoryService.GetLatestByGroupIdAsync(project.Id);
            Log.Information("Latest report lookup result: {HasReport}", latestReport != null);

            if (latestReport == null)
            {
                // No saved report - generate one automatically
                Log.Information("No saved report found, generating new report for {ProjectName}", project.Name);
                ReportStatusMessage = "No saved report found. Generating...";
                await RunReportAsync(project);
                return;
            }

            // Notify listeners to display the saved report
            if (OnViewProjectReport != null)
            {
                Log.Information("Invoking OnViewProjectReport for {ProjectName}", project.Name);
                await OnViewProjectReport.Invoke(latestReport, project);
            }
            else
            {
                Log.Warning("OnViewProjectReport event has no subscribers");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in ViewLatestReportAsync for project {ProjectName}", project.Name);
            ReportStatusMessage = $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Validates a project's PAT by fetching the latest commit.
    /// Updates the project's transient LastCommitHash and LastCommitDate properties.
    /// </summary>
    [RelayCommand]
    private async Task ValidateProjectAsync(ProjectInstance project)
    {
        Log.Information("ValidateProjectAsync called for project: {ProjectName}", project.Name);

        if (_sourceProviderFactory == null || _encryptionService == null)
        {
            Log.Warning("Source provider factory or encryption service not available");
            project.ValidationError = "Validation service unavailable";
            RefreshProjectInList(project);
            return;
        }

        if (string.IsNullOrEmpty(project.SourcePat))
        {
            project.ValidationError = "No PAT configured";
            RefreshProjectInList(project);
            return;
        }

        if (string.IsNullOrEmpty(project.SourceOrganization) || string.IsNullOrEmpty(project.SourceRepository))
        {
            project.ValidationError = "Missing org/repo config";
            RefreshProjectInList(project);
            return;
        }

        project.IsValidating = true;
        project.ValidationError = null;
        project.LastCommitHash = null;
        project.LastCommitDate = null;
        RefreshProjectInList(project);

        try
        {
            var provider = _sourceProviderFactory.GetProvider(project.SourceType);

            // Create a temporary SourceRepository for the provider
            var sourceRepo = new SourceRepository
            {
                Id = project.Id,
                SourceType = project.SourceType,
                Organization = project.SourceOrganization,
                Project = project.SourceProject,
                Repository = project.SourceRepository,
                AuthorIdentifier = string.Empty, // Get all commits, not filtered by author
                EncryptedPat = await _encryptionService.EncryptAsync(project.SourcePat),
                ApiEndpoint = project.ApiEndpointOverride
            };

            // Fetch commits from the last 90 days to find the latest one
            var since = DateTimeOffset.UtcNow.AddDays(-90);
            var until = DateTimeOffset.UtcNow;

            var commits = await provider.GetCommitsAsync(sourceRepo, since, until);
            var latestCommit = commits.OrderByDescending(c => c.CommittedAt).FirstOrDefault();

            if (latestCommit != null)
            {
                project.LastCommitHash = latestCommit.Sha.Length >= 8
                    ? latestCommit.Sha.Substring(0, 8)
                    : latestCommit.Sha;
                project.LastCommitDate = latestCommit.CommittedAt;
                Log.Information(
                    "Latest commit for {ProjectName}: {Hash} on {Date}",
                    project.Name,
                    project.LastCommitHash,
                    project.LastCommitDate);
            }
            else
            {
                project.ValidationError = "No commits in 90 days";
                Log.Information("No recent commits found for {ProjectName}", project.Name);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to validate project {ProjectName}", project.Name);
            project.ValidationError = ex.Message.Length > 30
                ? string.Concat(ex.Message.AsSpan(0, 30), "...")
                : ex.Message;
        }
        finally
        {
            project.IsValidating = false;
            RefreshProjectInList(project);
        }
    }

    /// <summary>
    /// Validates all projects in the list sequentially.
    /// </summary>
    [RelayCommand]
    private async Task ValidateAllProjectsAsync()
    {
        foreach (var project in Projects.ToList())
        {
            await ValidateProjectAsync(project);
        }
    }

    /// <summary>
    /// Forces a UI refresh for a specific project by replacing it in the collection.
    /// This is needed because the transient properties don't raise PropertyChanged.
    /// </summary>
    private void RefreshProjectInList(ProjectInstance project)
    {
        var index = Projects.IndexOf(project);
        if (index >= 0)
        {
            // Force UI refresh by notifying collection changed
            Projects[index] = project;
        }
    }
}

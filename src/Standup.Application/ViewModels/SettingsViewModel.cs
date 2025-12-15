using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Standup.Application.Interfaces;
using Standup.Application.Models;
using Standup.Domain.Enums;

namespace Standup.Application.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IProjectService _projectService;
    private readonly ILocalStandupService _localStandupService;

    [ObservableProperty]
    private ObservableCollection<ProjectInstance> _projects = new();

    [ObservableProperty]
    private ProjectInstance? _currentProject;

    [ObservableProperty]
    private string _userId = string.Empty;

    [ObservableProperty]
    private string _tenantId = string.Empty;

    [ObservableProperty]
    private string _apiEndpoint = string.Empty;

    // Source configuration
    [ObservableProperty]
    private bool _useLocalGeneration = true;

    [ObservableProperty]
    private SourceType _sourceType = SourceType.AzureDevOps;

    [ObservableProperty]
    private string _sourceOrganization = string.Empty;

    [ObservableProperty]
    private string _sourceProject = string.Empty;

    [ObservableProperty]
    private string _sourceRepository = string.Empty;

    [ObservableProperty]
    private string _sourcePat = string.Empty;

    [ObservableProperty]
    private string _authorIdentifier = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isTestingConnection;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    // AI Configuration (stored in Preferences, not per-project)
    [ObservableProperty]
    private string _aiEndpoint = string.Empty;

    [ObservableProperty]
    private string _aiDeploymentName = string.Empty;

    [ObservableProperty]
    private string _aiApiKey = string.Empty;

    [ObservableProperty]
    private bool _aiUseAzureIdentity;

    public List<SourceType> SourceTypes { get; } = [SourceType.AzureDevOps, SourceType.GitHub];

    /// <summary>
    /// Event to request AI settings from MAUI layer (Preferences).
    /// </summary>
    public event Func<(string Endpoint, string Deployment, string ApiKey)>? LoadAISettingsRequested;

    /// <summary>
    /// Event to save AI settings to MAUI layer (Preferences).
    /// </summary>
    public event Action<string, string, string>? SaveAISettingsRequested;

    public SettingsViewModel(IProjectService projectService, ILocalStandupService localStandupService)
    {
        _projectService = projectService;
        _localStandupService = localStandupService;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            // Load all projects for the picker
            var allProjects = await _projectService.GetProjectsAsync();
            Projects.Clear();
            foreach (var project in allProjects)
            {
                Projects.Add(project);
            }

            // Select the current project
            var current = await _projectService.GetCurrentProjectAsync();
            CurrentProject = Projects.FirstOrDefault(p => p.Id == current?.Id) ?? Projects.FirstOrDefault();

            LoadProjectSettings();

            // Load AI settings from MAUI Preferences
            LoadAISettings();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void LoadAISettings()
    {
        var settings = LoadAISettingsRequested?.Invoke();
        if (settings.HasValue)
        {
            AiEndpoint = settings.Value.Endpoint;
            AiDeploymentName = settings.Value.Deployment;
            AiApiKey = settings.Value.ApiKey;
            AiUseAzureIdentity = string.IsNullOrEmpty(AiApiKey);
        }
    }

    partial void OnCurrentProjectChanged(ProjectInstance? value)
    {
        if (value != null && !IsLoading)
        {
            LoadProjectSettings();
        }
    }

    private void LoadProjectSettings()
    {
        if (CurrentProject != null)
        {
            UserId = CurrentProject.UserId ?? string.Empty;
            TenantId = CurrentProject.TenantId ?? string.Empty;
            ApiEndpoint = CurrentProject.ApiEndpoint;

            // Load source configuration
            UseLocalGeneration = CurrentProject.UseLocalGeneration;
            SourceType = CurrentProject.SourceType;
            SourceOrganization = CurrentProject.SourceOrganization ?? string.Empty;
            SourceProject = CurrentProject.SourceProject ?? string.Empty;
            SourceRepository = CurrentProject.SourceRepository ?? string.Empty;
            SourcePat = CurrentProject.SourcePat ?? string.Empty;
            AuthorIdentifier = CurrentProject.AuthorIdentifier ?? string.Empty;
            StatusMessage = string.Empty;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (CurrentProject == null)
        {
            return;
        }

        var updated = CurrentProject with
        {
            UserId = UserId,
            TenantId = TenantId,
            ApiEndpoint = ApiEndpoint,
            UseLocalGeneration = UseLocalGeneration,
            SourceType = SourceType,
            SourceOrganization = SourceOrganization,
            SourceProject = SourceProject,
            SourceRepository = SourceRepository,
            SourcePat = SourcePat,
            AuthorIdentifier = AuthorIdentifier
        };

        await _projectService.UpdateProjectAsync(updated);
        CurrentProject = updated;

        // Save AI settings to MAUI Preferences
        SaveAISettingsRequested?.Invoke(AiEndpoint, AiDeploymentName, AiApiKey);

        StatusMessage = "Settings saved!";
    }

    /// <summary>
    /// Save only AI settings (separate from project settings).
    /// </summary>
    [RelayCommand]
    private void SaveAISettings()
    {
        SaveAISettingsRequested?.Invoke(AiEndpoint, AiDeploymentName, AiApiKey);
        AiUseAzureIdentity = string.IsNullOrEmpty(AiApiKey);
        StatusMessage = "AI settings saved! Restart app to apply changes.";
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        if (string.IsNullOrEmpty(SourceOrganization) ||
            string.IsNullOrEmpty(SourceProject) ||
            string.IsNullOrEmpty(SourceRepository) ||
            string.IsNullOrEmpty(SourcePat))
        {
            StatusMessage = "Please fill in all source fields.";
            return;
        }

        IsTestingConnection = true;
        StatusMessage = "Testing connection...";

        try
        {
            var isValid = await _localStandupService.ValidateConnectionAsync(
                SourceType,
                SourceOrganization,
                SourceProject,
                SourceRepository,
                SourcePat);

            StatusMessage = isValid ? "Connection successful!" : "Connection failed. Please check your settings.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsTestingConnection = false;
        }
    }
}

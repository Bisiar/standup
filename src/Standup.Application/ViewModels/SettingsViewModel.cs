using System.Collections.ObjectModel;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Standup.Application.Interfaces;
using Standup.Application.Models;
using Standup.Domain.Enums;

namespace Standup.Application.ViewModels;

/// <summary>
/// ViewModel for application settings including AI configuration, cache management, and per-project settings.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly IProjectService _projectService;
    private readonly ILocalStandupService _localStandupService;
    private IReportCacheService? _cacheService;

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

    [ObservableProperty]
    private bool _isValidatingAI;

    [ObservableProperty]
    private string _aiValidationResult = string.Empty;

    [ObservableProperty]
    private bool _aiValidationSuccess;

    // Cache statistics
    [ObservableProperty]
    private int _cacheEntryCount;

    [ObservableProperty]
    private string _cacheSizeDisplay = "0 KB";

    // Integration properties (infrastructure only - configuration UI coming in future tasks)
    [ObservableProperty]
    private bool _integrationsAvailable = true;

    [ObservableProperty]
    private string _integrationsMessage = "Integration infrastructure is ready. Configuration UI coming soon.";

    // CRM Configuration
    [ObservableProperty]
    private bool _crmEnabled;

    [ObservableProperty]
    private string _crmInstanceUrl = string.Empty;

    [ObservableProperty]
    private string _crmTenantId = string.Empty;

    [ObservableProperty]
    private string _crmClientId = string.Empty;

    [ObservableProperty]
    private string _crmClientSecret = string.Empty;

    [ObservableProperty]
    private bool _isTestingCrmConnection;

    [ObservableProperty]
    private string _crmValidationResult = string.Empty;

    [ObservableProperty]
    private bool _crmValidationSuccess;

    /// <summary>
    /// Gets the display text for the current authentication method.
    /// </summary>
    public string AuthMethodDisplay => string.IsNullOrEmpty(AiApiKey) ? "Azure Identity (Entra ID)" : "API Key";

    /// <summary>
    /// Gets the color for the authentication method display.
    /// </summary>
    public string AuthMethodColor => string.IsNullOrEmpty(AiApiKey) ? "#10B981" : "#3B82F6";

    /// <summary>
    /// Gets the application version.
    /// </summary>
    public string AppVersion => Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

    public List<SourceType> SourceTypes { get; } = [SourceType.AzureDevOps, SourceType.GitHub];

    /// <summary>
    /// Event to request AI settings from MAUI layer (Preferences).
    /// </summary>
    public event Func<(string Endpoint, string Deployment, string ApiKey)>? LoadAISettingsRequested;

    /// <summary>
    /// Event to save AI settings to MAUI layer (Preferences).
    /// </summary>
    public event Action<string, string, string>? SaveAISettingsRequested;

    /// <summary>
    /// Event to validate AI connection from MAUI layer.
    /// Returns (success, errorMessage).
    /// </summary>
    public event Func<Task<(bool Success, string Message)>>? ValidateAIConnectionRequested;

    /// <summary>
    /// Event to request CRM settings from MAUI layer (Preferences).
    /// </summary>
    public event Func<(bool Enabled, string InstanceUrl, string TenantId, string ClientId, string ClientSecret)>? LoadCrmSettingsRequested;

    /// <summary>
    /// Event to save CRM settings to MAUI layer (Preferences).
    /// </summary>
    public event Action<bool, string, string, string, string>? SaveCrmSettingsRequested;

    /// <summary>
    /// Event to validate CRM connection from MAUI layer.
    /// Returns (success, errorMessage).
    /// </summary>
    public event Func<Task<(bool Success, string Message)>>? ValidateCrmConnectionRequested;

    public SettingsViewModel(IProjectService projectService, ILocalStandupService localStandupService)
    {
        _projectService = projectService;
        _localStandupService = localStandupService;
    }

    /// <summary>
    /// Sets the cache service for cache management operations.
    /// </summary>
    /// <param name="cacheService">The report cache service.</param>
    public void SetCacheService(IReportCacheService? cacheService)
    {
        _cacheService = cacheService;
        RefreshCacheStats();
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024)
        {
            return $"{bytes} B";
        }

        if (bytes < 1024 * 1024)
        {
            return $"{bytes / 1024.0:F1} KB";
        }

        return $"{bytes / (1024.0 * 1024.0):F1} MB";
    }

    private void RefreshCacheStats()
    {
        if (_cacheService == null)
        {
            CacheEntryCount = 0;
            CacheSizeDisplay = "N/A";
            return;
        }

        var stats = _cacheService.GetStats();
        CacheEntryCount = stats.CachedReports;
        CacheSizeDisplay = FormatBytes(stats.ApproximateSizeBytes);
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

            // Load CRM settings from MAUI Preferences
            LoadCrmSettings();

            // Update auth method display after loading
            OnPropertyChanged(nameof(AuthMethodDisplay));
            OnPropertyChanged(nameof(AuthMethodColor));

            // Load cache statistics
            RefreshCacheStats();
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

    private void LoadCrmSettings()
    {
        var settings = LoadCrmSettingsRequested?.Invoke();
        if (settings.HasValue)
        {
            CrmEnabled = settings.Value.Enabled;
            CrmInstanceUrl = settings.Value.InstanceUrl;
            CrmTenantId = settings.Value.TenantId;
            CrmClientId = settings.Value.ClientId;
            CrmClientSecret = settings.Value.ClientSecret;
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

        // Save CRM settings to MAUI Preferences
        SaveCrmSettingsRequested?.Invoke(CrmEnabled, CrmInstanceUrl, CrmTenantId, CrmClientId, CrmClientSecret);

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
        OnPropertyChanged(nameof(AuthMethodDisplay));
        OnPropertyChanged(nameof(AuthMethodColor));
        StatusMessage = "AI settings saved! Restart app to apply changes.";
    }

    /// <summary>
    /// Validate AI connection with current settings.
    /// </summary>
    [RelayCommand]
    private async Task ValidateAIConnectionAsync()
    {
        if (string.IsNullOrEmpty(AiEndpoint) || string.IsNullOrEmpty(AiDeploymentName))
        {
            AiValidationResult = "Please enter endpoint and deployment name.";
            AiValidationSuccess = false;
            return;
        }

        IsValidatingAI = true;
        AiValidationResult = "Validating...";
        AiValidationSuccess = false;

        try
        {
            var result = await (ValidateAIConnectionRequested?.Invoke() ?? Task.FromResult((false, "Validation not available")));
            AiValidationSuccess = result.Success;
            AiValidationResult = result.Message;
        }
        catch (Exception ex)
        {
            AiValidationSuccess = false;
            AiValidationResult = $"Error: {ex.Message}";
        }
        finally
        {
            IsValidatingAI = false;
        }
    }

    /// <summary>
    /// Clears all cached reports.
    /// </summary>
    [RelayCommand]
    private async Task ClearCacheAsync()
    {
        if (_cacheService == null)
        {
            StatusMessage = "Cache service not available.";
            return;
        }

        await _cacheService.InvalidateAllAsync();
        RefreshCacheStats();
        StatusMessage = "Cache cleared successfully!";
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

    /// <summary>
    /// Save only CRM settings (separate from project settings).
    /// </summary>
    [RelayCommand]
    private void SaveCrmSettings()
    {
        SaveCrmSettingsRequested?.Invoke(CrmEnabled, CrmInstanceUrl, CrmTenantId, CrmClientId, CrmClientSecret);
        StatusMessage = "CRM settings saved!";
    }

    /// <summary>
    /// Validate CRM connection with current settings.
    /// </summary>
    [RelayCommand]
    private async Task ValidateCrmConnectionAsync()
    {
        if (!CrmEnabled)
        {
            CrmValidationResult = "CRM integration is disabled.";
            CrmValidationSuccess = false;
            return;
        }

        if (string.IsNullOrEmpty(CrmInstanceUrl) || string.IsNullOrEmpty(CrmTenantId) ||
            string.IsNullOrEmpty(CrmClientId) || string.IsNullOrEmpty(CrmClientSecret))
        {
            CrmValidationResult = "Please enter all CRM connection details.";
            CrmValidationSuccess = false;
            return;
        }

        IsTestingCrmConnection = true;
        CrmValidationResult = "Validating CRM connection...";
        CrmValidationSuccess = false;

        try
        {
            var result = await (ValidateCrmConnectionRequested?.Invoke() ?? Task.FromResult((false, "Validation not available")));
            CrmValidationSuccess = result.Success;
            CrmValidationResult = result.Message;
        }
        catch (Exception ex)
        {
            CrmValidationSuccess = false;
            CrmValidationResult = $"Error: {ex.Message}";
        }
        finally
        {
            IsTestingCrmConnection = false;
        }
    }
}

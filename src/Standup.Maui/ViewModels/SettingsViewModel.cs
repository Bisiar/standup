using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Standup.Domain.Enums;
using Standup.Maui.Models;
using Standup.Maui.Services;

namespace Standup.Maui.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IProjectService _projectService;
    private readonly ILocalStandupService _localStandupService;

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

    public List<SourceType> SourceTypes { get; } = [SourceType.AzureDevOps, SourceType.GitHub];

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
            CurrentProject = await _projectService.GetCurrentProjectAsync();
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
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (CurrentProject == null)
            return;

        CurrentProject.UserId = UserId;
        CurrentProject.TenantId = TenantId;
        CurrentProject.ApiEndpoint = ApiEndpoint;

        // Save source configuration
        CurrentProject.UseLocalGeneration = UseLocalGeneration;
        CurrentProject.SourceType = SourceType;
        CurrentProject.SourceOrganization = SourceOrganization;
        CurrentProject.SourceProject = SourceProject;
        CurrentProject.SourceRepository = SourceRepository;
        CurrentProject.SourcePat = SourcePat;
        CurrentProject.AuthorIdentifier = AuthorIdentifier;

        await _projectService.UpdateProjectAsync(CurrentProject);
        StatusMessage = "Settings saved!";
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

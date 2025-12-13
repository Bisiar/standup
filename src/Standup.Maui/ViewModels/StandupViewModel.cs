using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Standup.Application.DTOs;
using Standup.Maui.Models;
using Standup.Maui.Services;

namespace Standup.Maui.ViewModels;

public partial class StandupViewModel : ObservableObject
{
    private readonly IProjectService _projectService;
    private readonly IStandupApiClient _apiClient;
    private readonly ILocalStandupService _localStandupService;

    [ObservableProperty]
    private ProjectInstance? _currentProject;

    [ObservableProperty]
    private StandupReportDto? _latestReport;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isGenerating;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public StandupViewModel(
        IProjectService projectService,
        IStandupApiClient apiClient,
        ILocalStandupService localStandupService)
    {
        _projectService = projectService;
        _apiClient = apiClient;
        _localStandupService = localStandupService;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            CurrentProject = await _projectService.GetCurrentProjectAsync();

            if (CurrentProject != null && !CurrentProject.UseLocalGeneration)
            {
                _apiClient.SetProject(CurrentProject.ApiEndpoint, CurrentProject.AccessToken);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task GenerateStandupAsync()
    {
        if (CurrentProject == null)
        {
            StatusMessage = "Please select a project first.";
            return;
        }

        IsGenerating = true;
        StatusMessage = "Generating standup report...";

        try
        {
            if (CurrentProject.UseLocalGeneration)
            {
                await GenerateLocalStandupAsync();
            }
            else
            {
                await GenerateApiStandupAsync();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsGenerating = false;
        }
    }

    private async Task GenerateLocalStandupAsync()
    {
        if (string.IsNullOrEmpty(CurrentProject!.SourceOrganization) ||
            string.IsNullOrEmpty(CurrentProject.SourceProject) ||
            string.IsNullOrEmpty(CurrentProject.SourceRepository) ||
            string.IsNullOrEmpty(CurrentProject.SourcePat))
        {
            StatusMessage = "Please configure your source repository in settings.";
            return;
        }

        LatestReport = await _localStandupService.GenerateStandupAsync(
            CurrentProject.SourceType,
            CurrentProject.SourceOrganization,
            CurrentProject.SourceProject,
            CurrentProject.SourceRepository,
            CurrentProject.SourcePat,
            CurrentProject.AuthorIdentifier);

        StatusMessage = $"Generated at {LatestReport.GeneratedAt:HH:mm}";
    }

    private async Task GenerateApiStandupAsync()
    {
        if (string.IsNullOrEmpty(CurrentProject!.UserId) || string.IsNullOrEmpty(CurrentProject.TenantId))
        {
            StatusMessage = "Please configure your user credentials in settings.";
            return;
        }

        LatestReport = await _apiClient.GenerateStandupAsync(
            CurrentProject.UserId,
            CurrentProject.TenantId);

        StatusMessage = $"Generated at {LatestReport.GeneratedAt:HH:mm}";
    }

    [RelayCommand]
    private async Task CopyToClipboardAsync()
    {
        if (LatestReport != null)
        {
            await Clipboard.Default.SetTextAsync(LatestReport.Summary);
            StatusMessage = "Copied to clipboard!";
        }
    }
}

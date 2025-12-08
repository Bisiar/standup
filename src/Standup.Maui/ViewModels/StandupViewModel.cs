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

    public StandupViewModel(IProjectService projectService, IStandupApiClient apiClient)
    {
        _projectService = projectService;
        _apiClient = apiClient;
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

        if (string.IsNullOrEmpty(CurrentProject.UserId) || string.IsNullOrEmpty(CurrentProject.TenantId))
        {
            StatusMessage = "Please configure your user credentials in settings.";
            return;
        }

        IsGenerating = true;
        StatusMessage = "Generating standup report...";

        try
        {
            LatestReport = await _apiClient.GenerateStandupAsync(
                CurrentProject.UserId,
                CurrentProject.TenantId);

            StatusMessage = $"Generated at {LatestReport.GeneratedAt:HH:mm}";
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

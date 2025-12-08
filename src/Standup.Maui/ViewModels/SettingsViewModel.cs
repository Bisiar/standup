using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Standup.Maui.Models;
using Standup.Maui.Services;

namespace Standup.Maui.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IProjectService _projectService;

    [ObservableProperty]
    private ProjectInstance? _currentProject;

    [ObservableProperty]
    private string _userId = string.Empty;

    [ObservableProperty]
    private string _tenantId = string.Empty;

    [ObservableProperty]
    private string _apiEndpoint = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public SettingsViewModel(IProjectService projectService)
    {
        _projectService = projectService;
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

        await _projectService.UpdateProjectAsync(CurrentProject);
        StatusMessage = "Settings saved!";
    }
}

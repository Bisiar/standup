using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Standup.Maui.Models;
using Standup.Maui.Services;

namespace Standup.Maui.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IProjectService _projectService;

    [ObservableProperty]
    private ProjectInstance? _currentProject;

    [ObservableProperty]
    private bool _isLoading;

    public MainViewModel(IProjectService projectService)
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
        }
        finally
        {
            IsLoading = false;
        }
    }
}

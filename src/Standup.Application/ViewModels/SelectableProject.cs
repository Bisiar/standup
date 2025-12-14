using CommunityToolkit.Mvvm.ComponentModel;
using Standup.Application.Models;

namespace Standup.Application.ViewModels;

/// <summary>
/// Wrapper for ProjectInstance with selection state for adding to a Group.
/// </summary>
public partial class SelectableProject : ObservableObject
{
    private readonly ProjectInstance _project;
    private readonly Func<SelectableProject, Task> _onSelectionChanged;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private string _clientCode = string.Empty;

    public SelectableProject(ProjectInstance project, bool isSelected, string clientCode, Func<SelectableProject, Task> onSelectionChanged)
    {
        _project = project;
        _isSelected = isSelected;
        _clientCode = clientCode;
        _onSelectionChanged = onSelectionChanged;
    }

    partial void OnIsSelectedChanged(bool value)
    {
        _onSelectionChanged?.Invoke(this);
    }

    public string Id => _project.Id;
    public string Name => _project.Name;
    public string DisplayName => $"{_project.SourceOrganization}/{_project.SourceProject ?? _project.SourceRepository}";
    public string SourceTypeDisplay => _project.SourceType.ToString();
    public ProjectInstance Project => _project;
}

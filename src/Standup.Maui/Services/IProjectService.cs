using Standup.Maui.Models;

namespace Standup.Maui.Services;

public interface IProjectService
{
    Task<IEnumerable<ProjectInstance>> GetProjectsAsync();
    Task<ProjectInstance?> GetCurrentProjectAsync();
    Task<ProjectInstance> AddProjectAsync(ProjectInstance project);
    Task<ProjectInstance> UpdateProjectAsync(ProjectInstance project);
    Task DeleteProjectAsync(string projectId);
    Task SetCurrentProjectAsync(string projectId);
}

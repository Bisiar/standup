// <copyright file="JsonFileProjectService.cs" company="Standup">
// Copyright (c) Standup. All rights reserved.
// </copyright>

using System.Text.Json;
using Standup.Application.Interfaces;
using Standup.Application.Models;

namespace Standup.Blazor.Services;

/// <summary>
/// JSON file-based implementation of IProjectService for Blazor Server.
/// </summary>
public class JsonFileProjectService : IProjectService
{
    private readonly string _projectsFilePath;
    private readonly string _currentProjectFilePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private List<ProjectInstance> _projects = new();
    private string? _currentProjectId;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonFileProjectService"/> class.
    /// </summary>
    /// <param name="configuration">Application configuration.</param>
    public JsonFileProjectService(IConfiguration configuration)
    {
        var dataPath = configuration["DataPath"] ?? "./data";
        Directory.CreateDirectory(dataPath);
        _projectsFilePath = Path.Combine(dataPath, "projects.json");
        _currentProjectFilePath = Path.Combine(dataPath, "current-project.txt");

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        LoadAsync().GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ProjectInstance>> GetProjectsAsync()
    {
        await _lock.WaitAsync();
        try
        {
            return _projects.ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<ProjectInstance?> GetCurrentProjectAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (string.IsNullOrEmpty(_currentProjectId))
            {
                return _projects.FirstOrDefault(p => p.IsDefault) ?? _projects.FirstOrDefault();
            }

            return _projects.FirstOrDefault(p => p.Id == _currentProjectId);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<ProjectInstance> AddProjectAsync(ProjectInstance project)
    {
        await _lock.WaitAsync();
        try
        {
            _projects.Add(project);
            await SaveProjectsAsync();
            return project;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<ProjectInstance> UpdateProjectAsync(ProjectInstance project)
    {
        await _lock.WaitAsync();
        try
        {
            var index = _projects.FindIndex(p => p.Id == project.Id);
            if (index >= 0)
            {
                _projects[index] = project;
                await SaveProjectsAsync();
            }

            return project;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task DeleteProjectAsync(string projectId)
    {
        await _lock.WaitAsync();
        try
        {
            _projects.RemoveAll(p => p.Id == projectId);
            if (_currentProjectId == projectId)
            {
                _currentProjectId = null;
                await SaveCurrentProjectAsync();
            }

            await SaveProjectsAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task SetCurrentProjectAsync(string projectId)
    {
        await _lock.WaitAsync();
        try
        {
            _currentProjectId = projectId;
            await SaveCurrentProjectAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task LoadAsync()
    {
        if (File.Exists(_projectsFilePath))
        {
            var json = await File.ReadAllTextAsync(_projectsFilePath);
            _projects = JsonSerializer.Deserialize<List<ProjectInstance>>(json, _jsonOptions) ?? new();
        }

        if (File.Exists(_currentProjectFilePath))
        {
            _currentProjectId = await File.ReadAllTextAsync(_currentProjectFilePath);
            _currentProjectId = _currentProjectId?.Trim();
        }
    }

    private async Task SaveProjectsAsync()
    {
        var json = JsonSerializer.Serialize(_projects, _jsonOptions);
        await File.WriteAllTextAsync(_projectsFilePath, json);
    }

    private async Task SaveCurrentProjectAsync()
    {
        if (string.IsNullOrEmpty(_currentProjectId))
        {
            if (File.Exists(_currentProjectFilePath))
            {
                File.Delete(_currentProjectFilePath);
            }
        }
        else
        {
            await File.WriteAllTextAsync(_currentProjectFilePath, _currentProjectId);
        }
    }
}

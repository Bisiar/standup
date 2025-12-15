using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using Standup.Application.Interfaces;
using Standup.Application.Services;
using Standup.Domain.Entities;
using Standup.Domain.Enums;

namespace Standup.Application.ViewModels;

/// <summary>
/// ViewModel for adding a repository to a group with PAT-based discovery.
/// </summary>
public partial class AddRepositoryViewModel : ObservableObject
{
    private readonly GroupService _groupService;
    private readonly CredentialService _credentialService;
    private readonly IRepositoryDiscoveryService _discoveryService;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger _logger;

    private string? _groupId;

    [ObservableProperty]
    private string _clientCode = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _recentClientCodes = new();

    [ObservableProperty]
    private SourceType _sourceType = SourceType.AzureDevOps;

    [ObservableProperty]
    private string _organization = string.Empty;

    [ObservableProperty]
    private string _pat = string.Empty;

    [ObservableProperty]
    private string? _project;

    [ObservableProperty]
    private ObservableCollection<string> _availableProjects = new();

    [ObservableProperty]
    private DiscoveredRepository? _selectedRepository;

    [ObservableProperty]
    private ObservableCollection<DiscoveredRepository> _availableRepositories = new();

    [ObservableProperty]
    private string? _authorIdentifier;

    [ObservableProperty]
    private string? _localPath;

    [ObservableProperty]
    private bool _useRepoPat;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isDiscovering;

    [ObservableProperty]
    private bool _patValidated;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _hasExistingOrgCredential;

    public AddRepositoryViewModel(
        GroupService groupService,
        CredentialService credentialService,
        IRepositoryDiscoveryService discoveryService,
        IEncryptionService encryptionService,
        ILogger logger)
    {
        _groupService = groupService;
        _credentialService = credentialService;
        _discoveryService = discoveryService;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    /// <summary>
    /// Initialize the ViewModel for adding a repository to a specific group.
    /// </summary>
    /// <param name="groupId">The ID of the group to add the repository to.</param>
    /// <param name="recentClientCodes">Optional list of recent client codes for suggestions.</param>
    public void Initialize(string groupId, IEnumerable<string>? recentClientCodes = null)
    {
        _groupId = groupId;

        RecentClientCodes.Clear();
        if (recentClientCodes != null)
        {
            foreach (var code in recentClientCodes.Take(5))
            {
                RecentClientCodes.Add(code);
            }
        }

        Reset();
    }

    private void Reset()
    {
        ClientCode = string.Empty;
        Organization = string.Empty;
        Pat = string.Empty;
        Project = null;
        SelectedRepository = null;
        AuthorIdentifier = null;
        LocalPath = null;
        UseRepoPat = false;
        PatValidated = false;
        HasExistingOrgCredential = false;
        StatusMessage = string.Empty;

        AvailableProjects.Clear();
        AvailableRepositories.Clear();
    }

    partial void OnSourceTypeChanged(SourceType value)
    {
        // Clear project-related fields when switching to GitHub
        if (value == SourceType.GitHub)
        {
            Project = null;
            AvailableProjects.Clear();
        }

        // Reset discovery state
        PatValidated = false;
        AvailableRepositories.Clear();
        SelectedRepository = null;
    }

    partial void OnOrganizationChanged(string value)
    {
        // Reset validation when org changes
        PatValidated = false;
        AvailableRepositories.Clear();
        SelectedRepository = null;

        // Check for existing credential asynchronously
        _ = CheckExistingCredentialAsync();
    }

    private async Task CheckExistingCredentialAsync()
    {
        if (string.IsNullOrWhiteSpace(Organization))
        {
            HasExistingOrgCredential = false;
            return;
        }

        try
        {
            var credential = await _credentialService.GetCredentialAsync(SourceType, Organization);
            HasExistingOrgCredential = credential != null;

            if (HasExistingOrgCredential)
            {
                StatusMessage = "Using existing organization credential";
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to check existing credential for {Org}", Organization);
            HasExistingOrgCredential = false;
        }
    }

    [RelayCommand]
    private void SelectRecentClientCode(string code)
    {
        ClientCode = code;
    }

    [RelayCommand]
    private async Task ValidateAndDiscoverAsync()
    {
        if (string.IsNullOrWhiteSpace(Organization))
        {
            StatusMessage = "Organization is required";
            return;
        }

        // Use existing credential PAT if no new PAT provided
        var patToUse = Pat;
        if (string.IsNullOrWhiteSpace(patToUse) && HasExistingOrgCredential)
        {
            var credential = await _credentialService.GetCredentialAsync(SourceType, Organization);
            if (credential != null)
            {
                patToUse = await _encryptionService.DecryptAsync(credential.EncryptedPat);
            }
        }

        if (string.IsNullOrWhiteSpace(patToUse))
        {
            StatusMessage = "PAT is required for authentication";
            return;
        }

        IsDiscovering = true;
        StatusMessage = "Validating credentials...";

        try
        {
            var isValid = await _discoveryService.ValidatePatAsync(SourceType, Organization, patToUse);

            if (!isValid)
            {
                StatusMessage = "Invalid credentials. Please check your PAT and organization.";
                PatValidated = false;
                return;
            }

            PatValidated = true;
            StatusMessage = "Credentials validated! Discovering repositories...";

            // Save credential if new PAT provided
            if (!string.IsNullOrWhiteSpace(Pat) && !HasExistingOrgCredential)
            {
                await _credentialService.SaveCredentialAsync(SourceType, Organization, Pat);
                HasExistingOrgCredential = true;
                Pat = string.Empty; // Clear the visible PAT after saving
            }

            // For Azure DevOps, discover projects first
            if (SourceType == SourceType.AzureDevOps)
            {
                var projects = await _discoveryService.GetProjectsAsync(SourceType, Organization, patToUse);

                AvailableProjects.Clear();
                foreach (var proj in projects)
                {
                    AvailableProjects.Add(proj);
                }

                if (AvailableProjects.Any())
                {
                    StatusMessage = $"Found {AvailableProjects.Count} projects. Select one to see repositories.";
                }
                else
                {
                    StatusMessage = "No projects found in this organization.";
                }
            }
            else
            {
                // For GitHub, discover repos directly
                await DiscoverRepositoriesAsync(patToUse);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to validate PAT for {SourceType} {Org}", SourceType, Organization);
            StatusMessage = $"Error: {ex.Message}";
            PatValidated = false;
        }
        finally
        {
            IsDiscovering = false;
        }
    }

    partial void OnProjectChanged(string? value)
    {
        if (!string.IsNullOrEmpty(value) && PatValidated)
        {
            _ = DiscoverRepositoriesForProjectAsync();
        }
    }

    private async Task DiscoverRepositoriesForProjectAsync()
    {
        if (string.IsNullOrWhiteSpace(Project) || !PatValidated)
        {
            return;
        }

        IsDiscovering = true;
        StatusMessage = $"Discovering repositories in {Project}...";

        try
        {
            var patToUse = Pat;
            if (string.IsNullOrWhiteSpace(patToUse) && HasExistingOrgCredential)
            {
                var credential = await _credentialService.GetCredentialAsync(SourceType, Organization);
                if (credential != null)
                {
                    patToUse = await _encryptionService.DecryptAsync(credential.EncryptedPat);
                }
            }

            await DiscoverRepositoriesAsync(patToUse!, Project);
        }
        finally
        {
            IsDiscovering = false;
        }
    }

    private async Task DiscoverRepositoriesAsync(string pat, string? project = null)
    {
        try
        {
            var repos = await _discoveryService.GetRepositoriesAsync(
                SourceType, Organization, project, pat);

            AvailableRepositories.Clear();
            foreach (var repo in repos)
            {
                AvailableRepositories.Add(repo);
            }

            if (AvailableRepositories.Any())
            {
                StatusMessage = $"Found {AvailableRepositories.Count} repositories";
            }
            else
            {
                StatusMessage = "No repositories found";
            }
        }
        catch (Exception ex)
        {
            _logger.Error(
                ex,
                "Failed to discover repositories for {SourceType} {Org}/{Project}",
                SourceType,
                Organization,
                project);
            StatusMessage = $"Error discovering repositories: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task AddRepositoryAsync()
    {
        if (string.IsNullOrWhiteSpace(_groupId))
        {
            StatusMessage = "No group selected";
            return;
        }

        if (SelectedRepository == null)
        {
            StatusMessage = "Please select a repository";
            return;
        }

        IsLoading = true;
        StatusMessage = "Adding repository...";

        try
        {
            string? encryptedPat = null;

            // Only encrypt repo-level PAT if user explicitly wants to override org PAT
            if (UseRepoPat && !string.IsNullOrWhiteSpace(Pat))
            {
                encryptedPat = _encryptionService.Encrypt(Pat);
            }

            var repository = new GroupedRepository
            {
                ClientCode = ClientCode.ToUpperInvariant(),
                SourceType = SelectedRepository.SourceType,
                Organization = SelectedRepository.Organization,
                Project = SelectedRepository.Project,
                Repository = SelectedRepository.Name,
                EncryptedPat = encryptedPat,
                AuthorIdentifier = AuthorIdentifier,
                LocalPath = LocalPath,
                IsActive = true
            };

            await _groupService.AddRepositoryToGroupAsync(_groupId, repository);

            StatusMessage = "Repository added successfully!";

            // Notify success - view should handle navigation
            OnRepositoryAdded?.Invoke(this, repository);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to add repository to group {GroupId}", _groupId);
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        OnCancelled?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Event raised when a repository is successfully added.
    /// </summary>
    public event EventHandler<GroupedRepository>? OnRepositoryAdded;

    /// <summary>
    /// Event raised when the user cancels.
    /// </summary>
    public event EventHandler? OnCancelled;
}

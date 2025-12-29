using Standup.Application.ViewModels;
using Standup.Domain.Interfaces;

namespace Standup.Maui.Views.Components;

/// <summary>
/// Settings tab view for global application settings.
/// </summary>
public partial class SettingsTabView : ContentView
{
    private SettingsViewModel? _currentViewModel;
    private IAISummaryService? _aiService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsTabView"/> class.
    /// </summary>
    public SettingsTabView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Sets the AI service for validation.
    /// </summary>
    /// <param name="aiService">The AI summary service.</param>
    public void SetAIService(IAISummaryService? aiService)
    {
        _aiService = aiService;
    }

    /// <inheritdoc/>
    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        // Unsubscribe from previous view model if any
        if (_currentViewModel != null)
        {
            _currentViewModel.LoadAISettingsRequested -= OnLoadAISettingsRequested;
            _currentViewModel.SaveAISettingsRequested -= OnSaveAISettingsRequested;
            _currentViewModel.ValidateAIConnectionRequested -= OnValidateAIConnectionRequested;
        }

        // Subscribe to new view model
        if (BindingContext is SettingsViewModel viewModel)
        {
            _currentViewModel = viewModel;
            viewModel.LoadAISettingsRequested += OnLoadAISettingsRequested;
            viewModel.SaveAISettingsRequested += OnSaveAISettingsRequested;
            viewModel.ValidateAIConnectionRequested += OnValidateAIConnectionRequested;
        }
        else
        {
            _currentViewModel = null;
        }
    }

    private (string Endpoint, string Deployment, string ApiKey) OnLoadAISettingsRequested()
    {
        return (
            Preferences.Get("AIFoundry__Endpoint", string.Empty),
            Preferences.Get("AIFoundry__DeploymentName", string.Empty),
            Preferences.Get("AIFoundry__ApiKey", string.Empty));
    }

    private void OnSaveAISettingsRequested(string endpoint, string deployment, string apiKey)
    {
        Preferences.Set("AIFoundry__Endpoint", endpoint);
        Preferences.Set("AIFoundry__DeploymentName", deployment);
        Preferences.Set("AIFoundry__ApiKey", apiKey);
    }

    private async Task<(bool Success, string Message)> OnValidateAIConnectionRequested()
    {
        if (_aiService == null)
        {
            return (false, "AI service not configured. Restart app after saving settings.");
        }

        try
        {
            // Create minimal test data
            var testCommit = new Domain.Entities.CommitInfo(
                Sha: "abc1234",
                Message: "Test commit",
                Repository: "test",
                SourceType: Domain.Enums.SourceType.GitHub,
                CommittedAt: DateTimeOffset.Now);

            var testData = new Domain.Entities.StandupData
            {
                Commits = [testCommit],
                PullRequests = [],
                WorkItems = [],
            };

            var options = new Domain.Interfaces.SummaryOptions(
                Type: Domain.Enums.SummaryType.Technical,
                MaxLength: 50);

            // This will test authentication and connectivity
            var result = await _aiService.GenerateSummaryAsync(testData, options);

            if (!string.IsNullOrEmpty(result))
            {
                return (true, "✓ Connection successful! AI service is working.");
            }

            return (false, "Connection succeeded but no response received.");
        }
        catch (Azure.Identity.CredentialUnavailableException ex)
        {
            if (ex.Message.Contains("AzureCliCredential"))
            {
                return (false, "Azure CLI not found. Run: brew install azure-cli && az login");
            }

            return (false, $"Authentication failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (false, $"Connection failed: {ex.Message}");
        }
    }
}

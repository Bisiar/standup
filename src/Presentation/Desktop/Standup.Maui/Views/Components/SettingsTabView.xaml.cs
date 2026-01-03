using Microsoft.Extensions.DependencyInjection;
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
            _currentViewModel.LoadCrmSettingsRequested -= OnLoadCrmSettingsRequested;
            _currentViewModel.SaveCrmSettingsRequested -= OnSaveCrmSettingsRequested;
            _currentViewModel.ValidateCrmConnectionRequested -= OnValidateCrmConnectionRequested;
        }

        // Subscribe to new view model
        if (BindingContext is SettingsViewModel viewModel)
        {
            _currentViewModel = viewModel;
            viewModel.LoadAISettingsRequested += OnLoadAISettingsRequested;
            viewModel.SaveAISettingsRequested += OnSaveAISettingsRequested;
            viewModel.ValidateAIConnectionRequested += OnValidateAIConnectionRequested;
            viewModel.LoadCrmSettingsRequested += OnLoadCrmSettingsRequested;
            viewModel.SaveCrmSettingsRequested += OnSaveCrmSettingsRequested;
            viewModel.ValidateCrmConnectionRequested += OnValidateCrmConnectionRequested;
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

    private (bool Enabled, string InstanceUrl, string TenantId, string ClientId, string ClientSecret) OnLoadCrmSettingsRequested()
    {
        return (
            Preferences.Get("DynamicsCrm__Enabled", false),
            Preferences.Get("DynamicsCrm__InstanceUrl", string.Empty),
            Preferences.Get("DynamicsCrm__TenantId", string.Empty),
            Preferences.Get("DynamicsCrm__ClientId", string.Empty),
            Preferences.Get("DynamicsCrm__ClientSecret", string.Empty));
    }

    private void OnSaveCrmSettingsRequested(bool enabled, string instanceUrl, string tenantId, string clientId, string clientSecret)
    {
        Preferences.Set("DynamicsCrm__Enabled", enabled);
        Preferences.Set("DynamicsCrm__InstanceUrl", instanceUrl);
        Preferences.Set("DynamicsCrm__TenantId", tenantId);
        Preferences.Set("DynamicsCrm__ClientId", clientId);
        Preferences.Set("DynamicsCrm__ClientSecret", clientSecret);
    }

    private async Task<(bool Success, string Message)> OnValidateCrmConnectionRequested()
    {
        var crmService = Microsoft.Maui.Controls.Application.Current?.Handler?.MauiContext?.Services.GetService<ICrmProjectService>();
        if (crmService == null)
        {
            return (false, "CRM service not available. Restart app after saving settings.");
        }

        try
        {
            var isValid = await crmService.ValidateConnectionAsync();
            if (isValid)
            {
                return (true, "✓ CRM connection successful!");
            }

            return (false, "CRM connection failed. Check your settings.");
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

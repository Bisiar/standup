using Standup.Application.ViewModels;

namespace Standup.Maui.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        // Wire up AI settings events to MAUI Preferences
        viewModel.LoadAISettingsRequested += () =>
        {
            return (
                Preferences.Get("AIFoundry__Endpoint", string.Empty),
                Preferences.Get("AIFoundry__DeploymentName", string.Empty),
                Preferences.Get("AIFoundry__ApiKey", string.Empty));
        };

        viewModel.SaveAISettingsRequested += (endpoint, deployment, apiKey) =>
        {
            Preferences.Set("AIFoundry__Endpoint", endpoint);
            Preferences.Set("AIFoundry__DeploymentName", deployment);
            Preferences.Set("AIFoundry__ApiKey", apiKey);
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is SettingsViewModel vm)
        {
            await vm.LoadCommand.ExecuteAsync(null);
        }
    }
}

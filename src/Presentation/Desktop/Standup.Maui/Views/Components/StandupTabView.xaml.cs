using Standup.Application.ViewModels;
using Standup.Domain.Entities;

namespace Standup.Maui.Views.Components;

public partial class StandupTabView : ContentView
{
    public StandupTabView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handles the Switch.Toggled event for repository inclusion.
    /// Calls the ViewModel to save changes and trigger on-the-fly generation if needed.
    /// </summary>
    private async void OnRepoInclusionToggled(object? sender, ToggledEventArgs e)
    {
        if (sender is not Switch toggleSwitch)
        {
            return;
        }

        if (toggleSwitch.BindingContext is not GroupedRepository repo)
        {
            return;
        }

        if (BindingContext is not StandupViewModel viewModel)
        {
            return;
        }

        await viewModel.OnRepoInclusionChangedAsync(repo);
    }
}

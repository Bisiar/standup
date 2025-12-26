using Standup.Application.ViewModels;
using Standup.Domain.Enums;

namespace Standup.Maui.Views;

public partial class AddRepositoryPage : ContentPage
{
    private readonly AddRepositoryViewModel _viewModel;

    public AddRepositoryPage(AddRepositoryViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

        viewModel.OnRepositoryAdded += OnRepositoryAdded;
        viewModel.OnCancelled += OnCancelled;
    }

    /// <summary>
    /// Initialize the page for a specific group.
    /// </summary>
    /// <param name="groupId">The ID of the group to add the repository to.</param>
    /// <param name="recentClientCodes">Optional list of recent client codes for suggestions.</param>
    public void Initialize(string groupId, IEnumerable<string>? recentClientCodes = null)
    {
        _viewModel.Initialize(groupId, recentClientCodes);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.OnRepositoryAdded -= OnRepositoryAdded;
        _viewModel.OnCancelled -= OnCancelled;
    }

    private void OnSourceTypeChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (sender is RadioButton radioButton && e.Value)
        {
            _viewModel.SourceType = radioButton.Content?.ToString() == "GitHub"
                ? SourceType.GitHub
                : SourceType.AzureDevOps;
        }
    }

    private async void OnRepositoryAdded(object? sender, Domain.Entities.GroupedRepository e)
    {
        await Navigation.PopAsync();
    }

    private async void OnCancelled(object? sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}

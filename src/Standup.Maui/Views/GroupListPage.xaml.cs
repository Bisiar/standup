using Standup.Application.ViewModels;

namespace Standup.Maui.Views;

public partial class GroupListPage : ContentPage
{
    private readonly GroupListViewModel _viewModel;

    public GroupListPage(GroupListViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadGroupsCommand.ExecuteAsync(null);
    }
}

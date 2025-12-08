using Standup.Maui.ViewModels;

namespace Standup.Maui.Views;

public partial class RepositoryConfigPage : ContentPage
{
    public RepositoryConfigPage(RepositoryConfigViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is RepositoryConfigViewModel vm)
        {
            await vm.LoadCommand.ExecuteAsync(null);
        }
    }
}

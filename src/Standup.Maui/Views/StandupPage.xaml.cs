using Standup.Maui.ViewModels;

namespace Standup.Maui.Views;

public partial class StandupPage : ContentPage
{
    public StandupPage(StandupViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is StandupViewModel vm)
        {
            await vm.LoadCommand.ExecuteAsync(null);
        }
    }
}

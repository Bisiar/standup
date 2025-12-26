using Standup.Application.ViewModels;

namespace Standup.Maui.Views;

public partial class ProjectListPage : ContentPage
{
    public ProjectListPage(ProjectListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ProjectListViewModel vm)
        {
            await vm.LoadProjectsCommand.ExecuteAsync(null);
        }
    }
}

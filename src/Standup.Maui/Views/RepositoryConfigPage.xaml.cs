using Serilog;
using Standup.Application.ViewModels;
using Standup.Domain.Entities;

namespace Standup.Maui.Views;

public partial class RepositoryConfigPage : ContentPage
{
    public RepositoryConfigPage(RepositoryConfigViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        WireUpCloseEvent(viewModel);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RepositoryConfigPage"/> class.
    /// Constructor for opening config page for a specific group.
    /// </summary>
    /// <param name="group">The repository group to configure.</param>
    public RepositoryConfigPage(RepositoryGroup group)
    {
        InitializeComponent();
        Log.Information("RepositoryConfigPage created for group: {GroupName}", group.Name);

        // Get ViewModel from DI and set the group
        var vm = MauiProgram.ServiceProvider?.GetService<RepositoryConfigViewModel>();
        if (vm != null)
        {
            vm.SetGroup(group);
            BindingContext = vm;
            WireUpCloseEvent(vm);
        }
        else
        {
            Log.Error("Failed to get RepositoryConfigViewModel from DI");
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is RepositoryConfigViewModel vm)
        {
            await vm.LoadCommand.ExecuteAsync(null);
        }
    }

    private void WireUpCloseEvent(RepositoryConfigViewModel vm)
    {
        vm.OnClose += async (s, e) =>
        {
            Log.Information("RepositoryConfigPage closing");
            await Navigation.PopModalAsync();
        };
    }
}

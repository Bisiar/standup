using Serilog;
using Standup.Application.ViewModels;

namespace Standup.Maui.Views;

/// <summary>
/// Manual tabbed page implementation for MacCatalyst since TabbedPage doesn't render tabs.
/// Shell's TabBar triggers Swift Observation crash in libswiftObservation.dylib.
/// See: https://github.com/dotnet/maui/issues/31982
///
/// All UI is defined in XAML using ContentViews (StandupTabView, GroupsTabView, etc.).
/// Tab navigation and state management is handled by FrameworkViewModel.
/// </summary>
public partial class MainTabbedPage : ContentPage
{
    private readonly FrameworkViewModel _viewModel;

    public MainTabbedPage(FrameworkViewModel viewModel)
    {
        Log.Information("MainTabbedPage constructor starting");

        _viewModel = viewModel;
        BindingContext = _viewModel;

        InitializeComponent();

        Log.Information("MainTabbedPage InitializeComponent completed");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        Log.Information("MainTabbedPage.OnAppearing - initializing FrameworkViewModel");

        try
        {
            await _viewModel.InitializeCommand.ExecuteAsync(null);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error initializing FrameworkViewModel");
        }
    }
}

using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Standup.Maui.Views;

/// <summary>
/// Manual tabbed page implementation for MacCatalyst since TabbedPage doesn't render tabs.
/// Uses a Grid with content area and bottom tab bar.
/// Shell's TabBar triggers Swift Observation crash in libswiftObservation.dylib.
/// See: https://github.com/dotnet/maui/issues/31982
/// </summary>
public partial class MainTabbedPage : ContentPage
{
    private readonly Grid _contentArea;
    private readonly HorizontalStackLayout _tabBar;
    private readonly List<(Button Button, Page Page)> _tabs = new();
    private int _selectedIndex = -1;

    public MainTabbedPage()
    {
        Log.Information("MainTabbedPage constructor starting");

        _contentArea = new Grid();
        _tabBar = new HorizontalStackLayout
        {
            HorizontalOptions = LayoutOptions.Center,
            Spacing = 10,
            Padding = new Thickness(10)
        };

        Content = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Star },
                new RowDefinition { Height = GridLength.Auto }
            },
            Children =
            {
                _contentArea,
                _tabBar
            }
        };

        Grid.SetRow(_contentArea, 0);
        Grid.SetRow(_tabBar, 1);

        // Defer page loading to avoid ServiceProvider null issue
        Dispatcher.Dispatch(LoadPages);

        Log.Information("MainTabbedPage InitializeComponent completed");
    }

    private void LoadPages()
    {
        var serviceProvider = MauiProgram.ServiceProvider;
        if (serviceProvider == null)
        {
            Log.Error("ServiceProvider is null in MainTabbedPage.LoadPages");
            _contentArea.Children.Add(new Label { Text = "Failed to load - ServiceProvider is null" });
            return;
        }

        try
        {
            Log.Information("Adding StandupPage tab");
            var standupPage = serviceProvider.GetRequiredService<StandupPage>();
            AddTab("Standup", standupPage);

            Log.Information("Adding ProjectListPage tab");
            var projectsPage = serviceProvider.GetRequiredService<ProjectListPage>();
            AddTab("Projects", projectsPage);

            Log.Information("Adding SettingsPage tab");
            var settingsPage = serviceProvider.GetRequiredService<SettingsPage>();
            AddTab("Settings", settingsPage);

            // Select first tab
            if (_tabs.Count > 0)
                SelectTab(0);

            Log.Information("MainTabbedPage loaded with {Count} tabs", _tabs.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to create tabs in MainTabbedPage");
            _contentArea.Children.Add(new Label { Text = $"Failed to load: {ex.Message}" });
        }
    }

    private void AddTab(string title, Page page)
    {
        var button = new Button
        {
            Text = title,
            BackgroundColor = Colors.DimGray,
            TextColor = Colors.White,
            Padding = new Thickness(20, 10),
            CornerRadius = 5
        };

        var index = _tabs.Count;
        button.Clicked += (s, e) => SelectTab(index);

        _tabs.Add((button, page));
        _tabBar.Children.Add(button);
    }

    private void SelectTab(int index)
    {
        if (index < 0 || index >= _tabs.Count) return;
        if (index == _selectedIndex) return;

        // Update button styles
        for (int i = 0; i < _tabs.Count; i++)
        {
            _tabs[i].Button.BackgroundColor = i == index ? Color.FromArgb("#512BD4") : Colors.DimGray;
        }

        // Show selected page content
        _contentArea.Children.Clear();
        var selectedPage = _tabs[index].Page;

        // Wrap the page content in a view
        if (selectedPage is ContentPage contentPage && contentPage.Content != null)
        {
            _contentArea.Children.Add(contentPage.Content);
        }

        _selectedIndex = index;
        Title = _tabs[index].Button.Text;
    }
}

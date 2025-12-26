using System.ComponentModel;
using Standup.Application.ViewModels;
using Telerik.Maui.Controls.Compatibility.Chart;

namespace Standup.Maui.Views.Components;

/// <summary>
/// Code-behind for the Dashboard tab view that displays code metrics charts.
/// </summary>
public partial class DashboardTabView
{
    /// <summary>
    /// Color palette for project lines in the trend chart.
    /// </summary>
    private static readonly Color[] ProjectColors =
    [
        Color.FromArgb("#3B82F6"), // Blue
        Color.FromArgb("#10B981"), // Green
        Color.FromArgb("#F59E0B"), // Amber
        Color.FromArgb("#EF4444"), // Red
        Color.FromArgb("#8B5CF6"), // Purple
        Color.FromArgb("#EC4899"), // Pink
        Color.FromArgb("#06B6D4"), // Cyan
        Color.FromArgb("#84CC16"), // Lime
    ];

    /// <summary>
    /// Creates a legend item for the trend chart.
    /// </summary>
    /// <param name="projectName">The project name.</param>
    /// <param name="color">The line color.</param>
    /// <returns>A view representing the legend item.</returns>
    private static View CreateLegendItem(string projectName, Color color)
    {
        var stack = new HorizontalStackLayout
        {
            Spacing = 6,
            Margin = new Thickness(8, 2),
        };

        var colorIndicator = new BoxView
        {
            Color = color,
            WidthRequest = 16,
            HeightRequest = 4,
            CornerRadius = 2,
            VerticalOptions = LayoutOptions.Center,
        };

        var label = new Label
        {
            Text = projectName,
            FontSize = 12,
            TextColor = Color.FromArgb("#94A3B8"),
            VerticalOptions = LayoutOptions.Center,
        };

        stack.Children.Add(colorIndicator);
        stack.Children.Add(label);

        return stack;
    }

    private DashboardViewModel? _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="DashboardTabView"/> class.
    /// </summary>
    public DashboardTabView()
    {
        InitializeComponent();
        BindingContextChanged += OnBindingContextChanged;
    }

    private void OnBindingContextChanged(object? sender, EventArgs e)
    {
        // Unsubscribe from old view model
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        // Subscribe to new view model
        _viewModel = BindingContext as DashboardViewModel;
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            // Initial refresh if data is already available
            if (_viewModel.ProjectDailyActivities.Count > 0)
            {
                RefreshTrendChartSeries();
            }
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DashboardViewModel.ProjectDailyActivities))
        {
            RefreshTrendChartSeries();
        }
    }

    /// <summary>
    /// Refreshes the trend chart with dynamic LineSeries for each project.
    /// </summary>
    private void RefreshTrendChartSeries()
    {
        if (_viewModel == null || DailyTrendChart == null)
        {
            return;
        }

        // Clear existing series
        DailyTrendChart.Series.Clear();
        TrendChartLegend.Children.Clear();

        var projectData = _viewModel.ProjectDailyActivities;
        if (projectData.Count == 0)
        {
            return;
        }

        int colorIndex = 0;
        foreach (var kvp in projectData)
        {
            var projectName = kvp.Key;
            var dailyData = kvp.Value;
            var color = ProjectColors[colorIndex % ProjectColors.Length];

            // Create LineSeries for this project
            var lineSeries = new LineSeries
            {
                ValueBinding = new PropertyNameDataPointBinding("TotalActivity"),
                CategoryBinding = new PropertyNameDataPointBinding("Date"),
                ItemsSource = dailyData,
                Stroke = color,
                StrokeThickness = 3,
                DisplayName = projectName,
            };

            DailyTrendChart.Series.Add(lineSeries);

            // Add legend item
            var legendItem = CreateLegendItem(projectName, color);
            TrendChartLegend.Children.Add(legendItem);

            colorIndex++;
        }
    }
}

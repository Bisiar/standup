using Serilog;
using Standup.Maui.Views;

namespace Standup.Maui;

public partial class App : Microsoft.Maui.Controls.Application
{
    public App()
    {
        try
        {
            Log.Information("App constructor starting");
            InitializeComponent();
            Log.Information("App.InitializeComponent completed");

            // Use TabbedPage instead of Shell to avoid Swift Observation crash on macOS 26 Tahoe
            // Shell's TabBar triggers crash in libswiftObservation.dylib
            // See: https://github.com/dotnet/maui/issues/31982
            MainPage = new MainTabbedPage();
            Log.Information("MainTabbedPage created and set as MainPage");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "FATAL: App constructor failed");
            throw;
        }
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        try
        {
            Log.Information("CreateWindow called");
            var window = base.CreateWindow(activationState);
            Log.Information("Window created successfully");
            return window;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "FATAL: CreateWindow failed");
            throw;
        }
    }
}

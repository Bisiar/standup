using Microsoft.Extensions.DependencyInjection;
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

            // Use TabbedPage instead of Shell to avoid Swift Observation crash on macOS 26 Tahoe
            // Shell's TabBar triggers crash in libswiftObservation.dylib
            // See: https://github.com/dotnet/maui/issues/31982

            // Resolve MainTabbedPage from DI (it requires FrameworkViewModel)
            var mainPage = MauiProgram.ServiceProvider?.GetRequiredService<MainTabbedPage>()
                ?? throw new InvalidOperationException("Failed to resolve MainTabbedPage from DI");

            var window = new Window(mainPage);
            Log.Information("Window created with MainTabbedPage");
            return window;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "FATAL: CreateWindow failed");
            throw;
        }
    }
}

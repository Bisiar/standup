using Serilog;

namespace Standup.Maui;

public partial class AppShell : Shell
{
    public AppShell()
    {
        try
        {
            Log.Information("AppShell constructor starting");
            InitializeComponent();
            Log.Information("AppShell.InitializeComponent completed");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "FATAL: AppShell constructor failed");
            throw;
        }
    }
}

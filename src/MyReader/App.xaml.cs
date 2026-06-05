using Microsoft.UI.Xaml;
using MyReader.Services;
using System.Diagnostics;

namespace MyReader;

public partial class App : Application
{
    private Window? _window;

    public static DatabaseService Database { get; private set; } = null!;
    public static Window? MainWindow { get; private set; }

    public App()
    {
        try
        {
            Debug.WriteLine("=== App constructor started ===");

            // 全局异常处理
            UnhandledException += (sender, e) =>
            {
                Debug.WriteLine($"!!! Unhandled exception: {e.Exception}");
                e.Handled = true;
                try
                {
                    var logPath = Path.Combine(AppContext.BaseDirectory, "crash.log");
                    File.WriteAllText(logPath, $"Unhandled exception:\n{e.Exception}");
                }
                catch { }
            };

            Debug.WriteLine("Calling InitializeComponent...");
            InitializeComponent();
            Debug.WriteLine("InitializeComponent completed");

            Debug.WriteLine("Creating DatabaseService...");
            Database = new DatabaseService();
            Debug.WriteLine("DatabaseService created");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"!!! App constructor FAILED: {ex}");
            try
            {
                var logPath = Path.Combine(AppContext.BaseDirectory, "crash.log");
                File.WriteAllText(logPath, $"App constructor failed:\n{ex}");
            }
            catch { }
            throw;
        }
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            Debug.WriteLine("=== OnLaunched started ===");
            _window = new MainWindow();
            MainWindow = _window;
            _window.Activate();
            Debug.WriteLine("Window activated");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"!!! OnLaunched FAILED: {ex}");
            try
            {
                var logPath = Path.Combine(AppContext.BaseDirectory, "crash.log");
                File.WriteAllText(logPath, $"OnLaunched failed:\n{ex}");
            }
            catch { }
            throw;
        }
    }
}

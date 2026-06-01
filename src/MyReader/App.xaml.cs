using Microsoft.UI.Xaml;
using MyReader.Services;

namespace MyReader;

public partial class App : Application
{
    private Window? _window;

    public static DatabaseService Database { get; private set; } = null!;

    public App()
    {
        InitializeComponent();
        Database = new DatabaseService();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}

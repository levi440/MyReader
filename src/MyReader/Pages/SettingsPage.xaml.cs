using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;

namespace MyReader.Pages;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
        Loaded += SettingsPage_Loaded;
    }

    private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        // 设置主题
        var theme = Application.Current.RequestedTheme;
        ThemeComboBox.SelectedIndex = theme switch
        {
            ApplicationTheme.Light => 0,
            ApplicationTheme.Dark => 1,
            _ => 2
        };

        // 显示数据路径
        var dataPath = Path.Combine(AppContext.BaseDirectory, "data");
        DataPathText.Text = dataPath;

        // 显示数据库大小
        var dbPath = Path.Combine(dataPath, "reader.db");
        if (File.Exists(dbPath))
        {
            var size = new FileInfo(dbPath).Length;
            DbSizeText.Text = size switch
            {
                < 1024 => $"{size} B",
                < 1024 * 1024 => $"{size / 1024:F1} KB",
                _ => $"{size / (1024 * 1024):F1} MB"
            };
        }
        else
        {
            DbSizeText.Text = "未创建";
        }
    }

    private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThemeComboBox.SelectedItem is ComboBoxItem item)
        {
            var theme = item.Tag?.ToString();
            if (theme == "Light")
            {
                Application.Current.RequestedTheme = ApplicationTheme.Light;
            }
            else if (theme == "Dark")
            {
                Application.Current.RequestedTheme = ApplicationTheme.Dark;
            }
            // "Default" 跟随系统，不需要设置
        }
    }

    private async void OpenDataFolder_Click(object sender, RoutedEventArgs e)
    {
        var dataPath = Path.Combine(AppContext.BaseDirectory, "data");
        if (Directory.Exists(dataPath))
        {
            await Windows.System.Launcher.LaunchFolderPathAsync(dataPath);
        }
    }
}

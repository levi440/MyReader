using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MyReader.Services;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace MyReader.Pages;

public sealed partial class SettingsPage : Page
{
    private readonly BookSourceImportService _importService;
    private readonly FeedService _feedService;

    public SettingsPage()
    {
        InitializeComponent();
        _importService = new BookSourceImportService(App.Database);
        _feedService = new FeedService(App.Database);
        Loaded += SettingsPage_Loaded;
    }

    private async void SettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        // 设置主题
        var theme = Application.Current.RequestedTheme;
        ThemeComboBox.SelectedIndex = theme switch
        {
            ApplicationTheme.Light => 0,
            ApplicationTheme.Dark => 1,
            _ => 2
        };

        // 读取保存的设置
        await LoadSettings();

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

        // 显示书籍数量
        var books = await App.Database.GetAllBooksAsync();
        BookCountText.Text = $"{books.Count} 本";

        // 显示书源数量
        var sources = await _importService.GetAllBookSourcesAsync();
        SourceCountText.Text = $"{sources.Count} 个（{sources.Count(s => s.Enabled)} 个已启用）";

        // 显示订阅数量
        var feeds = await _feedService.GetAllFeedsAsync();
        FeedCountText.Text = $"{feeds.Count} 个";
    }

    private async Task LoadSettings()
    {
        // 从数据库读取设置
        using var conn = App.Database.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Key, Value FROM Settings";

        var settings = new Dictionary<string, string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            settings[reader.GetString(0)] = reader.GetString(1);
        }

        // 应用设置
        if (settings.TryGetValue("DefaultFontSize", out var fontSize) && int.TryParse(fontSize, out var size))
        {
            DefaultFontSizeSlider.Value = size;
            FontSizeLabel.Text = size.ToString();
        }

        if (settings.TryGetValue("DefaultLineHeight", out var lineHeight) && int.TryParse(lineHeight, out var height))
        {
            DefaultLineHeightSlider.Value = height;
            LineHeightLabel.Text = height.ToString();
        }
    }

    private async Task SaveSetting(string key, string value)
    {
        using var conn = App.Database.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT OR REPLACE INTO Settings (Key, Value) VALUES (@Key, @Value)
            """;
        cmd.Parameters.AddWithValue("@Key", key);
        cmd.Parameters.AddWithValue("@Value", value);
        await cmd.ExecuteNonQueryAsync();
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

    private async void DefaultFontSizeSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        var value = (int)e.NewValue;
        FontSizeLabel.Text = value.ToString();
        await SaveSetting("DefaultFontSize", value.ToString());
    }

    private async void DefaultLineHeightSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        var value = (int)e.NewValue;
        LineHeightLabel.Text = value.ToString();
        await SaveSetting("DefaultLineHeight", value.ToString());
    }

    private async void OpenDataFolder_Click(object sender, RoutedEventArgs e)
    {
        var dataPath = Path.Combine(AppContext.BaseDirectory, "data");
        if (Directory.Exists(dataPath))
        {
            await Windows.System.Launcher.LaunchFolderPathAsync(dataPath);
        }
        else
        {
            var dialog = new ContentDialog
            {
                Title = "提示",
                Content = "数据目录尚未创建",
                CloseButtonText = "确定",
                XamlRoot = XamlRoot
            };
            await dialog.ShowAsync();
        }
    }

    private async void ExportSources_Click(object sender, RoutedEventArgs e)
    {
        var sources = await _importService.GetAllBookSourcesAsync();
        if (sources.Count == 0)
        {
            await ShowMessageDialog("没有书源可导出");
            return;
        }

        var picker = new FileSavePicker();
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        picker.FileTypeChoices.Add("JSON 文件", new List<string> { ".json" });
        picker.SuggestedFileName = $"MyReader_书源_{DateTime.Now:yyyy-MM-dd}";

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSaveFileAsync();
        if (file != null)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(sources,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(file.Path, json);
            await ShowMessageDialog($"已导出 {sources.Count} 个书源");
        }
    }

    private async void ImportSources_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        picker.FileTypeFilter.Add(".json");

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();
        if (file != null)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file.Path);
                var count = await _importService.ImportFromJsonAsync(json);
                await ShowMessageDialog($"成功导入 {count} 个书源");

                // 刷新显示
                var sources = await _importService.GetAllBookSourcesAsync();
                SourceCountText.Text = $"{sources.Count} 个（{sources.Count(s => s.Enabled)} 个已启用）";
            }
            catch (Exception ex)
            {
                await ShowMessageDialog($"导入失败：{ex.Message}");
            }
        }
    }

    private async void ExportFeeds_Click(object sender, RoutedEventArgs e)
    {
        var feeds = await _feedService.GetAllFeedsAsync();
        if (feeds.Count == 0)
        {
            await ShowMessageDialog("没有订阅可导出");
            return;
        }

        var picker = new FileSavePicker();
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        picker.FileTypeChoices.Add("OPML 文件", new List<string> { ".opml" });
        picker.FileTypeChoices.Add("JSON 文件", new List<string> { ".json" });
        picker.SuggestedFileName = $"MyReader_订阅_{DateTime.Now:yyyy-MM-dd}";

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSaveFileAsync();
        if (file != null)
        {
            var ext = Path.GetExtension(file.Path).ToLowerInvariant();
            string content;

            if (ext == ".opml")
            {
                content = ExportFeedsAsOpml(feeds);
            }
            else
            {
                content = System.Text.Json.JsonSerializer.Serialize(feeds,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            }

            await File.WriteAllTextAsync(file.Path, content);
            await ShowMessageDialog($"已导出 {feeds.Count} 个订阅");
        }
    }

    private string ExportFeedsAsOpml(List<Models.FeedSource> feeds)
    {
        var opml = new System.Xml.Linq.XDocument(
            new System.Xml.Linq.XElement("opml",
                new System.Xml.Linq.XAttribute("version", "2.0"),
                new System.Xml.Linq.XElement("head",
                    new System.Xml.Linq.XElement("title", "MyReader Subscriptions")),
                new System.Xml.Linq.XElement("body",
                    feeds.Select(f => new System.Xml.Linq.XElement("outline",
                        new System.Xml.Linq.XAttribute("type", "rss"),
                        new System.Xml.Linq.XAttribute("text", f.Title),
                        new System.Xml.Linq.XAttribute("xmlUrl", f.Url))))));
        return opml.ToString();
    }

    private async void ImportFeeds_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        picker.FileTypeFilter.Add(".opml");
        picker.FileTypeFilter.Add(".xml");
        picker.FileTypeFilter.Add(".json");

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();
        if (file != null)
        {
            try
            {
                var content = await File.ReadAllTextAsync(file.Path);
                var ext = Path.GetExtension(file.Path).ToLowerInvariant();

                if (ext == ".opml" || ext == ".xml")
                {
                    // 解析 OPML
                    var doc = System.Xml.Linq.XDocument.Parse(content);
                    var outlines = doc.Descendants("outline")
                        .Where(o => o.Attribute("xmlUrl") != null);

                    int count = 0;
                    foreach (var outline in outlines)
                    {
                        var url = outline.Attribute("xmlUrl")?.Value;
                        if (!string.IsNullOrEmpty(url))
                        {
                            var feed = await _feedService.AddFeedAsync(url);
                            if (feed != null) count++;
                        }
                    }

                    await ShowMessageDialog($"成功导入 {count} 个订阅");
                }
                else
                {
                    // 解析 JSON
                    var feeds = System.Text.Json.JsonSerializer.Deserialize<List<Models.FeedSource>>(content);
                    if (feeds != null)
                    {
                        int count = 0;
                        foreach (var feed in feeds)
                        {
                            if (!string.IsNullOrEmpty(feed.Url))
                            {
                                var result = await _feedService.AddFeedAsync(feed.Url);
                                if (result != null) count++;
                            }
                        }
                        await ShowMessageDialog($"成功导入 {count} 个订阅");
                    }
                }

                // 刷新显示
                var feedList = await _feedService.GetAllFeedsAsync();
                FeedCountText.Text = $"{feedList.Count} 个";
            }
            catch (Exception ex)
            {
                await ShowMessageDialog($"导入失败：{ex.Message}");
            }
        }
    }

    private async Task ShowMessageDialog(string message)
    {
        var dialog = new ContentDialog
        {
            Title = "提示",
            Content = message,
            CloseButtonText = "确定",
            XamlRoot = XamlRoot
        };
        await dialog.ShowAsync();
    }
}

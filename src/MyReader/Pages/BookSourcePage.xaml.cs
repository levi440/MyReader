using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MyReader.Models;
using MyReader.Services;
using Windows.Storage.Pickers;

namespace MyReader.Pages;

public sealed partial class BookSourcePage : Page
{
    private readonly BookSourceImportService _importService;
    private readonly BookSourceEngine _engine;
    private List<BookSource> _sources = new();
    private BookSource? _selectedSource;

    public BookSourcePage()
    {
        InitializeComponent();
        _importService = new BookSourceImportService(App.Database);
        _engine = new BookSourceEngine();
        Loaded += BookSourcePage_Loaded;
    }

    private async void BookSourcePage_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadSources();
    }

    private async Task LoadSources()
    {
        _sources = await _importService.GetAllBookSourcesAsync();
        SourceList.ItemsSource = _sources.Select(s => $"{s.BookSourceName} ({s.BookSourceUrl})").ToList();

        SourceCountText.Text = $"共 {_sources.Count} 个书源，{_sources.Count(s => s.Enabled)} 个已启用";
        EmptyState.Visibility = _sources.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        SourceList.Visibility = _sources.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SourceList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is string itemStr)
        {
            var index = SourceList.Items.IndexOf(itemStr);
            if (index >= 0 && index < _sources.Count)
            {
                _selectedSource = _sources[index];
                ShowSourceDetail(_selectedSource);
            }
        }
    }

    private void SourceList_ContextRequested(UIElement sender, Microsoft.UI.Xaml.Input.ContextRequestedEventArgs args)
    {
        if (args.OriginalSource is FrameworkElement element)
        {
            var index = SourceList.Items.IndexOf(element.DataContext);
            if (index >= 0 && index < _sources.Count)
            {
                _selectedSource = _sources[index];
            }
        }
    }

    private void ShowSourceDetail(BookSource source)
    {
        DetailPanel.Visibility = Visibility.Visible;

        SourceNameBox.Text = source.BookSourceName;
        SourceUrlBox.Text = source.BookSourceUrl;
        SearchUrlBox.Text = source.SearchUrl ?? "";

        RuleSearchBox.Text = System.Text.Json.JsonSerializer.Serialize(source.RuleSearch,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        RuleTocBox.Text = System.Text.Json.JsonSerializer.Serialize(source.RuleToc,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        RuleContentBox.Text = System.Text.Json.JsonSerializer.Serialize(source.RuleContent,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }

    private async void ImportFromCommunity_Click(object sender, RoutedEventArgs e)
    {
        var repos = BookSourceImportService.GetCommunityRepos();
        var repoNames = repos.Select(r => r.Name).ToArray();

        var dialog = new ContentDialog
        {
            Title = "从社区仓库导入",
            PrimaryButtonText = "导入",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        var comboBox = new ComboBox
        {
            ItemsSource = repoNames,
            SelectedIndex = 0,
            MinWidth = 300
        };
        dialog.Content = comboBox;

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            var selectedIndex = comboBox.SelectedIndex;
            var progressBar = new ProgressBar
            {
                IsIndeterminate = true,
                VerticalAlignment = VerticalAlignment.Center
            };

            var loadingDialog = new ContentDialog
            {
                Title = "正在导入...",
                Content = progressBar,
                XamlRoot = XamlRoot
            };

            _ = loadingDialog.ShowAsync();

            try
            {
                var count = await _importService.ImportFromCommunityRepoAsync(selectedIndex);
                loadingDialog.Hide();

                await ShowMessageDialog($"成功导入 {count} 个书源");
                await LoadSources();
            }
            catch (Exception ex)
            {
                loadingDialog.Hide();
                await ShowMessageDialog($"导入失败：{ex.Message}");
            }
        }
    }

    private async void ImportFromUrl_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "从 URL 导入",
            PrimaryButtonText = "导入",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        var urlBox = new TextBox
        {
            PlaceholderText = "输入书源 JSON 的 URL",
            MinWidth = 400
        };
        dialog.Content = urlBox;

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && !string.IsNullOrEmpty(urlBox.Text))
        {
            try
            {
                var count = await _importService.ImportFromUrlAsync(urlBox.Text);
                await ShowMessageDialog($"成功导入 {count} 个书源");
                await LoadSources();
            }
            catch (Exception ex)
            {
                await ShowMessageDialog($"导入失败：{ex.Message}");
            }
        }
    }

    private async void ImportFromFile_Click(object sender, RoutedEventArgs e)
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
                await LoadSources();
            }
            catch (Exception ex)
            {
                await ShowMessageDialog($"导入失败：{ex.Message}");
            }
        }
    }

    private async void TestSearch_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedSource == null)
        {
            await ShowMessageDialog("请先选择一个书源");
            return;
        }

        var dialog = new ContentDialog
        {
            Title = $"测试搜索：{_selectedSource.BookSourceName}",
            PrimaryButtonText = "搜索",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        var keywordBox = new TextBox
        {
            PlaceholderText = "输入搜索关键词",
            MinWidth = 300
        };
        dialog.Content = keywordBox;

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && !string.IsNullOrEmpty(keywordBox.Text))
        {
            var progressBar = new ProgressBar
            {
                IsIndeterminate = true,
                VerticalAlignment = VerticalAlignment.Center
            };

            var loadingDialog = new ContentDialog
            {
                Title = "正在搜索...",
                Content = progressBar,
                XamlRoot = XamlRoot
            };

            _ = loadingDialog.ShowAsync();

            try
            {
                var searchResults = await _engine.SearchSingleAsync(_selectedSource, keywordBox.Text);
                loadingDialog.Hide();

                if (searchResults == null || searchResults.Count == 0)
                {
                    await ShowMessageDialog("没有找到结果");
                }
                else
                {
                    var resultText = string.Join("\n\n", searchResults.Take(5).Select(r =>
                        $"《{r.Name}》\n作者：{r.Author ?? "未知"}\n最新：{r.LastChapter ?? "无"}\n来源：{r.SourceName}"));

                    await ShowMessageDialog($"找到 {searchResults.Count} 条结果：\n\n{resultText}");
                }
            }
            catch (Exception ex)
            {
                loadingDialog.Hide();
                await ShowMessageDialog($"搜索失败：{ex.Message}");
            }
        }
    }

    private async void SaveSource_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedSource == null) return;

        _selectedSource.BookSourceName = SourceNameBox.Text;
        _selectedSource.BookSourceUrl = SourceUrlBox.Text;
        _selectedSource.SearchUrl = SearchUrlBox.Text;

        try
        {
            if (!string.IsNullOrWhiteSpace(RuleSearchBox.Text))
                _selectedSource.RuleSearch = System.Text.Json.JsonSerializer.Deserialize<RuleSearch>(RuleSearchBox.Text);
            if (!string.IsNullOrWhiteSpace(RuleTocBox.Text))
                _selectedSource.RuleToc = System.Text.Json.JsonSerializer.Deserialize<RuleToc>(RuleTocBox.Text);
            if (!string.IsNullOrWhiteSpace(RuleContentBox.Text))
                _selectedSource.RuleContent = System.Text.Json.JsonSerializer.Deserialize<RuleContent>(RuleContentBox.Text);
        }
        catch { }

        await _importService.SaveBookSourceAsync(_selectedSource);
        DetailPanel.Visibility = Visibility.Collapsed;
        await LoadSources();
        await ShowMessageDialog("保存成功");
    }

    private void CancelEdit_Click(object sender, RoutedEventArgs e)
    {
        DetailPanel.Visibility = Visibility.Collapsed;
    }

    private void SelectAll_Click(object sender, RoutedEventArgs e)
    {
        SourceList.SelectAll();
    }

    private void DeselectAll_Click(object sender, RoutedEventArgs e)
    {
        SourceList.SelectedItems.Clear();
    }

    private async void DeleteSelected_Click(object sender, RoutedEventArgs e)
    {
        var selectedItems = SourceList.SelectedItems;
        if (selectedItems.Count == 0)
        {
            await ShowMessageDialog("请先选择要删除的书源");
            return;
        }

        var dialog = new ContentDialog
        {
            Title = "删除书源",
            Content = $"确定要删除选中的 {selectedItems.Count} 个书源吗？",
            PrimaryButtonText = "删除",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            foreach (var item in selectedItems)
            {
                var index = SourceList.Items.IndexOf(item);
                if (index >= 0 && index < _sources.Count)
                {
                    await _importService.DeleteBookSourceAsync(_sources[index].Id);
                }
            }

            DetailPanel.Visibility = Visibility.Collapsed;
            await LoadSources();
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

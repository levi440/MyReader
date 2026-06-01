using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Navigation;
using MyReader.Helpers;
using MyReader.Models;

namespace MyReader.Pages;

public sealed partial class ReaderPage : Page
{
    private Book? _book;
    private string _currentTheme = "light";
    private int _fontSize = 18;
    private int _lineHeight = 30;
    private int _currentChapter = 0;
    private List<string> _chapters = new();
    private bool _isWebViewReady = false;

    public ReaderPage()
    {
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is Book book)
        {
            _book = book;
            TitleBlock.Text = book.Title;

            // 等待 WebView2 初始化完成
            await ReaderWebView.EnsureCoreWebView2Async();
            _isWebViewReady = true;

            await LoadBookContent();
        }
    }

    private async Task LoadBookContent()
    {
        if (_book == null || !_isWebViewReady) return;

        try
        {
            if (!File.Exists(_book.FilePath))
            {
                ShowError("文件不存在");
                return;
            }

            string content;
            if (_book.FileType == "txt")
            {
                content = await File.ReadAllTextAsync(_book.FilePath);
                _chapters = SplitChapters(content);
                if (_chapters.Count > 0)
                    content = _chapters[0];
            }
            else if (_book.FileType == "epub")
            {
                var epubBook = await VersOne.Epub.EpubReader.OpenBookAsync(_book.FilePath);
                _chapters = new List<string>();

                var htmlFiles = epubBook.Content.Html;
                if (htmlFiles != null)
                {
                    foreach (var pair in htmlFiles)
                    {
                        try
                        {
                            var html = await pair.Value.ReadContentAsTextAsync();
                            if (!string.IsNullOrEmpty(html))
                                _chapters.Add(html);
                        }
                        catch { }
                    }
                }

                content = _chapters.Count > 0 ? _chapters[0] : $"<h1>{epubBook.Title ?? _book.Title}</h1><p>EPUB 文件已加载，共 {_chapters.Count} 章</p>";
            }
            else
            {
                content = "<p>暂不支持此文件格式</p>";
            }

            var htmlContent = HtmlTemplateBuilder.BuildReadingHtml(content, _currentTheme, _fontSize, _lineHeight);
            ReaderWebView.NavigateToString(htmlContent);
        }
        catch (Exception ex)
        {
            ShowError($"加载失败：{ex.Message}");
        }
    }

    private void ShowError(string message)
    {
        var errorHtml = HtmlTemplateBuilder.BuildReadingHtml($"<p style='color: red;'>{message}</p>", _currentTheme, _fontSize, _lineHeight);
        ReaderWebView.NavigateToString(errorHtml);
    }

    private List<string> SplitChapters(string text)
    {
        var chapters = new List<string>();
        var parts = System.Text.RegularExpressions.Regex.Split(text, @"(第[一二三四五六七八九十百千0-9]+章\s*.*?[\r\n]+)");

        var current = "";
        foreach (var part in parts)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(part, @"^第[一二三四五六七八九十百千0-9]+章"))
            {
                if (!string.IsNullOrWhiteSpace(current))
                    chapters.Add(current);
                current = part;
            }
            else
            {
                current += part;
            }
        }

        if (!string.IsNullOrWhiteSpace(current))
            chapters.Add(current);

        if (chapters.Count == 0)
        {
            var chunkSize = 3000;
            for (int i = 0; i < text.Length; i += chunkSize)
            {
                var length = Math.Min(chunkSize, text.Length - i);
                chapters.Add(text.Substring(i, length));
            }
        }

        return chapters;
    }

    private void UpdateReadingView()
    {
        if (!_isWebViewReady || _chapters.Count == 0 || _currentChapter >= _chapters.Count) return;

        var content = _chapters[_currentChapter];
        var htmlContent = HtmlTemplateBuilder.BuildReadingHtml(content, _currentTheme, _fontSize, _lineHeight);
        ReaderWebView.NavigateToString(htmlContent);
    }

    private async void SaveProgress()
    {
        if (_book == null || _chapters.Count == 0) return;

        var progress = (double)_currentChapter / _chapters.Count * 100;
        await App.Database.UpdateBookProgressAsync(_book.Id, progress);
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        SaveProgress();
        if (Frame.CanGoBack)
            Frame.GoBack();
    }

    private void ThemeButton_Click(object sender, RoutedEventArgs e)
    {
        _currentTheme = _currentTheme switch
        {
            "light" => "sepia",
            "sepia" => "dark",
            _ => "light"
        };
        UpdateReadingView();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        SettingsPanel.Visibility = SettingsPanel.Visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private void PrevChapter_Click(object sender, RoutedEventArgs e)
    {
        if (_currentChapter > 0)
        {
            _currentChapter--;
            UpdateReadingView();
            SaveProgress();
        }
    }

    private void NextChapter_Click(object sender, RoutedEventArgs e)
    {
        if (_currentChapter < _chapters.Count - 1)
        {
            _currentChapter++;
            UpdateReadingView();
            SaveProgress();
        }
    }

    private void ProgressSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (_chapters.Count > 0)
        {
            var newChapter = (int)(e.NewValue / 100 * _chapters.Count);
            if (newChapter != _currentChapter && newChapter >= 0 && newChapter < _chapters.Count)
            {
                _currentChapter = newChapter;
                UpdateReadingView();
            }
        }
    }

    private void FontSizeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        _fontSize = (int)e.NewValue;
        UpdateReadingView();
    }

    private void LineHeightSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        _lineHeight = (int)e.NewValue;
        UpdateReadingView();
    }

    private void ThemeSelect_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string theme)
        {
            _currentTheme = theme;
            UpdateReadingView();
        }
    }
}

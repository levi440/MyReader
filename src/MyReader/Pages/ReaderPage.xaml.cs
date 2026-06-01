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
            await LoadBookContent();
        }
    }

    private async Task LoadBookContent()
    {
        if (_book == null) return;

        try
        {
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

                // 简单方式：读取所有 HTML 文件
                var htmlFiles = epubBook.Content.Html?.ToList();
                if (htmlFiles != null)
                {
                    foreach (var htmlFile in htmlFiles)
                    {
                        var html = await htmlFile.ReadContentAsTextAsync();
                        _chapters.Add(html);
                    }
                }

                content = _chapters.Count > 0 ? _chapters[0] : "<p>无法加载内容</p>";
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
            var errorHtml = HtmlTemplateBuilder.BuildReadingHtml($"<p>加载失败：{ex.Message}</p>", _currentTheme, _fontSize, _lineHeight);
            ReaderWebView.NavigateToString(errorHtml);
        }
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

        // 如果没有章节，按字数分割
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
        if (_chapters.Count > 0 && _currentChapter < _chapters.Count)
        {
            var content = _chapters[_currentChapter];
            var htmlContent = HtmlTemplateBuilder.BuildReadingHtml(content, _currentTheme, _fontSize, _lineHeight);
            ReaderWebView.NavigateToString(htmlContent);
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
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
        }
    }

    private void NextChapter_Click(object sender, RoutedEventArgs e)
    {
        if (_currentChapter < _chapters.Count - 1)
        {
            _currentChapter++;
            UpdateReadingView();
        }
    }

    private void ProgressSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        // 进度条逻辑
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

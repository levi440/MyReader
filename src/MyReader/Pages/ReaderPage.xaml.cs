using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Navigation;
using MyReader.Helpers;
using MyReader.Models;
using VersOne.Epub;

namespace MyReader.Pages;

public sealed partial class ReaderPage : Page
{
    private Book? _book;
    private EpubBookRef? _epubBookRef;
    private string _currentTheme = "light";
    private int _fontSize = 18;
    private int _lineHeight = 30;
    private int _currentChapter = 0;
    private List<string> _chapters = new();
    private bool _isWebViewReady = false;
    private bool _usePdfViewer = false;

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
                _epubBookRef = await EpubReader.OpenBookAsync(_book.FilePath);
                _chapters = new List<string>();

                // 读取图片数据（用于后续替换）
                var imageDataMap = new Dictionary<string, string>(); // key -> base64
                try
                {
                    var images = _epubBookRef.Content.Images;
                    if (images != null)
                    {
                        foreach (var pair in images)
                        {
                            try
                            {
                                var bytes = pair.Value.ReadContentAsBytes();
                                if (bytes != null && bytes.Length > 0)
                                {
                                    var ext = Path.GetExtension(pair.Key ?? "").ToLowerInvariant();
                                    var mimeType = ext switch
                                    {
                                        ".jpg" or ".jpeg" => "image/jpeg",
                                        ".png" => "image/png",
                                        ".gif" => "image/gif",
                                        ".webp" => "image/webp",
                                        ".svg" => "image/svg+xml",
                                        _ => "image/jpeg"
                                    };
                                    var base64 = $"data:{mimeType};base64,{Convert.ToBase64String(bytes)}";

                                    // 用完整 key 存
                                    if (!string.IsNullOrEmpty(pair.Key))
                                        imageDataMap[pair.Key] = base64;

                                    // 用文件名存
                                    var fileName = Path.GetFileName(pair.Key ?? "");
                                    if (!string.IsNullOrEmpty(fileName) && !imageDataMap.ContainsKey(fileName))
                                        imageDataMap[fileName] = base64;
                                }
                            }
                            catch { }
                        }
                    }
                }
                catch { }

                var htmlFiles = _epubBookRef.Content.Html;
                if (htmlFiles != null)
                {
                    foreach (var pair in htmlFiles)
                    {
                        try
                        {
                            var html = pair.Value.ReadContentAsText();
                            if (!string.IsNullOrEmpty(html))
                            {
                                // 替换图片路径为 base64
                                html = ReplaceImageSrc(html, imageDataMap);
                                _chapters.Add(html);
                            }
                        }
                        catch { }
                    }
                }

                content = _chapters.Count > 0 ? _chapters[0] : $"<h1>{_epubBookRef.Title ?? _book.Title}</h1><p>EPUB 文件已加载，共 {_chapters.Count} 章</p>";
            }
            else if (_book.FileType == "pdf")
            {
                // 尝试提取文本
                content = await Task.Run(() => ExtractPdfText(_book.FilePath));
                _chapters = SplitChapters(content);
                if (_chapters.Count > 0)
                    content = _chapters[0];

                // 如果提取的文本太少（可能是扫描版 PDF），使用 WebView2 内置 PDF 渲染
                if (content.Length < 100 || content.Contains("文本内容无法直接提取"))
                {
                    _usePdfViewer = true;
                }
            }
            else
            {
                content = "<p>暂不支持此文件格式</p>";
            }

            // 对于扫描版 PDF，使用 WebView2 内置 PDF 渲染
            if (_usePdfViewer && _book.FileType == "pdf")
            {
                var pdfUri = new Uri(_book.FilePath).AbsoluteUri;
                ReaderWebView.CoreWebView2.Navigate(pdfUri);
            }
            else
            {
                var htmlContent = HtmlTemplateBuilder.BuildReadingHtml(content, _currentTheme, _fontSize, _lineHeight);
                ReaderWebView.NavigateToString(htmlContent);
            }
        }
        catch (Exception ex)
        {
            ShowError($"加载失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 替换 HTML 中的图片 src 为 base64
    /// </summary>
    private string ReplaceImageSrc(string html, Dictionary<string, string> imageDataMap)
    {
        if (imageDataMap.Count == 0) return html;

        var imgRegex = new System.Text.RegularExpressions.Regex(
            @"src\s*=\s*[""']([^""']+)[""']",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return imgRegex.Replace(html, match =>
        {
            var originalSrc = match.Groups[1].Value;

            // 已经是 base64 或网络 URL，跳过
            if (originalSrc.StartsWith("data:") || originalSrc.StartsWith("http://") || originalSrc.StartsWith("https://"))
                return match.Value;

            // 精确匹配
            if (imageDataMap.TryGetValue(originalSrc, out var base64))
                return $"src=\"{base64}\"";

            // 用文件名匹配
            var fileName = Path.GetFileName(originalSrc);
            if (!string.IsNullOrEmpty(fileName) && imageDataMap.TryGetValue(fileName, out base64))
                return $"src=\"{base64}\"";

            // URL 解码后匹配
            var decoded = System.Web.HttpUtility.UrlDecode(originalSrc);
            if (decoded != originalSrc && imageDataMap.TryGetValue(decoded, out base64))
                return $"src=\"{base64}\"";

            return match.Value;
        });
    }

    /// <summary>
    /// 提取 PDF 文本内容
    /// </summary>
    private string ExtractPdfText(string pdfPath)
    {
        try
        {
            // 使用 Import 模式打开 PDF（PDFsharp 6.x 推荐方式）
            var document = PdfSharp.Pdf.IO.PdfReader.Open(pdfPath, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);
            var textBuilder = new System.Text.StringBuilder();

            for (int i = 0; i < document.PageCount; i++)
            {
                var page = document.Pages[i];

                try
                {
                    // 读取页面内容流
                    var content = PdfSharp.Pdf.Content.ContentReader.ReadContent(page);

                    // 遍历内容流提取文本操作符
                    ExtractTextFromContent(content as PdfSharp.Pdf.Content.Objects.CSequence ?? new PdfSharp.Pdf.Content.Objects.CSequence(), textBuilder);

                    textBuilder.AppendLine();
                    textBuilder.AppendLine($"--- 第 {i + 1} 页 ---");
                    textBuilder.AppendLine();
                }
                catch
                {
                    textBuilder.AppendLine($"[第 {i + 1} 页内容无法提取]");
                    textBuilder.AppendLine();
                }
            }

            document.Close();

            var result = textBuilder.ToString().Trim();

            if (string.IsNullOrWhiteSpace(result))
            {
                return $"""
                    <h1>{Path.GetFileNameWithoutExtension(pdfPath)}</h1>
                    <p>PDF 文件已加载，共 {document.PageCount} 页。</p>
                    <p>该 PDF 文件的文本内容无法直接提取（可能是扫描版 PDF）。</p>
                    <p>建议使用支持 OCR 的 PDF 阅读器打开此文件。</p>
                    """;
            }

            // 将纯文本转为 HTML 段落
            var htmlBuilder = new System.Text.StringBuilder();
            htmlBuilder.Append($"<h1>{Path.GetFileNameWithoutExtension(pdfPath)}</h1>");

            foreach (var line in result.Split('\n'))
            {
                var trimmed = line.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    if (trimmed.StartsWith("--- 第") && trimmed.EndsWith("页 ---"))
                        continue;

                    htmlBuilder.Append($"<p>{System.Net.WebUtility.HtmlEncode(trimmed)}</p>");
                }
            }

            return htmlBuilder.ToString();
        }
        catch (Exception ex)
        {
            return $"""
                <h1>{Path.GetFileNameWithoutExtension(pdfPath)}</h1>
                <p style="color: red;">PDF 加载失败：{ex.Message}</p>
                <p>请确保文件格式正确且未损坏。</p>
                """;
        }
    }

    /// <summary>
    /// 从 PDF 内容流中提取文本
    /// </summary>
    private void ExtractTextFromContent(PdfSharp.Pdf.Content.Objects.CSequence content, System.Text.StringBuilder textBuilder)
    {
        foreach (var item in content)
        {
            if (item is PdfSharp.Pdf.Content.Objects.COperator op)
            {
                var name = op.OpCode.Name;
                if (name == "Tj" || name == "'" || name == "\"")
                {
                    if (op.Operands.Count > 0)
                    {
                        var operand = op.Operands[0];
                        if (operand is PdfSharp.Pdf.Content.Objects.CString str)
                        {
                            textBuilder.Append(str.Value);
                        }
                    }
                }
                else if (name == "TJ")
                {
                    if (op.Operands.Count > 0)
                    {
                        var operand = op.Operands[0];
                        if (operand is PdfSharp.Pdf.Content.Objects.CArray arr)
                        {
                            foreach (var arrItem in arr)
                            {
                                if (arrItem is PdfSharp.Pdf.Content.Objects.CString str)
                                {
                                    textBuilder.Append(str.Value);
                                }
                            }
                        }
                    }
                }
                else if (name == "Td" || name == "TD" || name == "T*")
                {
                    textBuilder.AppendLine();
                }
            }
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

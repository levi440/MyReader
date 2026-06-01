using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.XPath;
using MyReader.Models;

namespace MyReader.Services;

/// <summary>
/// 书源规则引擎（兼容 Legado 3.0 格式）
/// </summary>
public partial class BookSourceEngine
{
    private readonly IBrowsingContext _context;
    private readonly HttpClient _httpClient;

    public BookSourceEngine()
    {
        var config = Configuration.Default
            .WithXPath()
            .WithDefaultLoader();
        _context = BrowsingContext.New(config);
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
    }

    /// <summary>
    /// 搜索书籍（并发查询所有启用的书源）
    /// </summary>
    public async Task<List<SearchResult>> SearchAllAsync(
        List<BookSource> sources, string keyword, int maxResults = 20)
    {
        var tasks = sources
            .Where(s => s.Enabled)
            .Select(s => SearchSingleAsync(s, keyword));

        var results = await Task.WhenAll(tasks);
        return results
            .Where(r => r != null)
            .SelectMany(r => r!)
            .Take(maxResults)
            .ToList();
    }

    /// <summary>
    /// 单个书源搜索
    /// </summary>
    public async Task<List<SearchResult>?> SearchSingleAsync(
        BookSource source, string keyword)
    {
        try
        {
            var searchUrl = source.SearchUrl ?? $"{source.BookSourceUrl}/search?q={{0}}";
            var url = string.Format(searchUrl, Uri.EscapeDataString(keyword));

            var html = await FetchHtmlAsync(url, source.Header);
            if (html == null) return null;

            var doc = await _context.OpenAsync(req => req.Content(html));
            var rules = source.RuleSearch;
            if (rules?.BookList == null) return null;

            var items = doc.QuerySelectorAll(rules.BookList);
            return items.Select(item => new SearchResult
            {
                SourceName = source.BookSourceName,
                Name = ExtractByRule(item, rules.Name) ?? "",
                Author = ExtractByRule(item, rules.Author),
                Cover = ToAbsoluteUrl(ExtractByRule(item, rules.Cover), source.BookSourceUrl),
                Kind = ExtractByRule(item, rules.Kind),
                LastChapter = ExtractByRule(item, rules.LastChapter),
                Intro = ExtractByRule(item, rules.Intro),
                BookUrl = ToAbsoluteUrl(ExtractByRule(item, rules.BookUrl), source.BookSourceUrl) ?? ""
            }).ToList();
        }
        catch
        {
            return null; // 一个书源挂了不影响其他
        }
    }

    /// <summary>
    /// 获取目录
    /// </summary>
    public async Task<List<ChapterInfo>> GetTocAsync(BookSource source, string bookUrl)
    {
        var html = await FetchHtmlAsync(bookUrl, source.Header);
        if (html == null) return new List<ChapterInfo>();

        var doc = await _context.OpenAsync(req => req.Content(html));
        var rules = source.RuleToc;
        if (rules?.ChapterList == null) return new List<ChapterInfo>();

        var items = doc.QuerySelectorAll(rules.ChapterList);
        return items.Select((item, index) => new ChapterInfo
        {
            Index = index,
            Title = ExtractByRule(item, rules.ChapterName) ?? "",
            Url = ToAbsoluteUrl(ExtractByRule(item, rules.ChapterUrl), source.BookSourceUrl) ?? "",
            IsVolume = ExtractByRule(item, rules.IsVolume)?.Contains("volume") ?? false,
            UpdateTime = ExtractByRule(item, rules.UpdateTime)
        }).ToList();
    }

    /// <summary>
    /// 获取正文
    /// </summary>
    public async Task<string> GetContentAsync(BookSource source, string chapterUrl)
    {
        var html = await FetchHtmlAsync(chapterUrl, source.Header);
        if (html == null) return "<p>无法加载内容</p>";

        var doc = await _context.OpenAsync(req => req.Content(html));
        var rules = source.RuleContent;
        if (rules?.Content == null) return "<p>规则配置错误</p>";

        var content = ExtractByRule(doc, rules.Content) ?? "";

        // 应用替换规则（去广告等）
        if (!string.IsNullOrEmpty(rules.ReplaceRegex))
        {
            foreach (var line in rules.ReplaceRegex.Split('\n'))
            {
                var parts = line.Split("@@", 2);
                if (parts.Length == 2)
                {
                    try
                    {
                        content = Regex.Replace(content, parts[0], parts[1]);
                    }
                    catch { }
                }
            }
        }

        return content;
    }

    /// <summary>
    /// 根据规则表达式提取内容
    /// </summary>
    private string? ExtractByRule(IParentNode element, string? rule)
    {
        if (string.IsNullOrEmpty(rule)) return null;

        if (rule.Contains('@'))
        {
            // CSS 选择器语法：div.title@text
            var parts = rule.Split('@', 2);
            var selector = parts[0];
            var attr = parts[1];
            var el = element.QuerySelector(selector);
            if (el == null) return null;

            return attr switch
            {
                "text" => el.TextContent?.Trim(),
                "html" => el.InnerHtml?.Trim(),
                "textNodes" => string.Concat(el.ChildNodes
                    .OfType<IText>()
                    .Select(n => n.Text)).Trim(),
                _ => el.GetAttribute(attr)?.Trim()
            };
        }
        else if (element is IDocument doc)
        {
            // XPath（需要 IDocument）
            try
            {
                var navigator = doc.CreateNavigator();
                var result = navigator?.Evaluate(rule);
                return result?.ToString()?.Trim();
            }
            catch
            {
                return null;
            }
        }
        else
        {
            // 对于非 IDocument 元素，尝试用 CSS 选择器
            var el = element.QuerySelector(rule);
            return el?.TextContent?.Trim();
        }
    }

    /// <summary>
    /// 获取 HTML 内容
    /// </summary>
    private async Task<string?> FetchHtmlAsync(string url, string? headerJson = null)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            // 应用自定义 Header
            if (!string.IsNullOrEmpty(headerJson))
            {
                // 简单解析 JSON header
                var headers = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(headerJson);
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }
            }

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 转换为绝对 URL
    /// </summary>
    private static string? ToAbsoluteUrl(string? url, string baseUrl)
    {
        if (string.IsNullOrEmpty(url)) return null;
        if (Uri.TryCreate(url, UriKind.Absolute, out _)) return url;
        if (Uri.TryCreate(new Uri(baseUrl), url, out var absoluteUri))
            return absoluteUri.ToString();
        return null;
    }
}

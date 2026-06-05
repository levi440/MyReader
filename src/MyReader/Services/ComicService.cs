using AngleSharp;
using MyReader.Models;
using SharpCompress.Archives;
using SharpCompress.Common;

namespace MyReader.Services;

/// <summary>
/// 漫画服务
/// </summary>
public class ComicService
{
    private readonly DatabaseService _db;
    private readonly IBrowsingContext _context;

    public ComicService(DatabaseService db)
    {
        _db = db;
        var config = Configuration.Default.WithDefaultLoader();
        _context = BrowsingContext.New(config);
    }

    /// <summary>
    /// 导入本地漫画文件（CBZ/CBR/ZIP）
    /// </summary>
    public async Task<Comic?> ImportLocalComicAsync(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        if (ext is not (".cbz" or ".cbr" or ".zip"))
            return null;

        var comic = new Comic
        {
            FilePath = filePath,
            SourceType = "local",
            Title = Path.GetFileNameWithoutExtension(filePath),
            AddedTime = DateTime.Now.ToString("O")
        };

        // 提取封面（第一张图片）
        try
        {
            var pages = ExtractPages(filePath);
            if (pages.Count > 0)
            {
                var coverDir = Path.Combine(AppContext.BaseDirectory, "data", "covers");
                Directory.CreateDirectory(coverDir);
                var coverPath = Path.Combine(coverDir, $"{comic.Id}.jpg");
                File.Copy(pages[0], coverPath, true);
                comic.CoverPath = coverPath;
            }
        }
        catch { }

        await SaveComicAsync(comic);
        return comic;
    }

    /// <summary>
    /// 解压漫画文件，返回图片路径列表（支持 ZIP/CBR/RAR）
    /// </summary>
    public List<string> ExtractPages(string filePath)
    {
        var tempDir = Path.Combine(
            Path.GetTempPath(), "MyReader", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            using var archive = ArchiveFactory.Open(filePath);

            var pages = new List<string>();
            foreach (var entry in archive.Entries)
            {
                if (entry.IsDirectory) continue;
                if (string.IsNullOrEmpty(entry.Key)) continue;

                var ext = Path.GetExtension(entry.Key).ToLowerInvariant();
                if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp" or ".gif")) continue;

                var outPath = Path.Combine(tempDir, entry.Key);
                var outDir = Path.GetDirectoryName(outPath);
                if (!string.IsNullOrEmpty(outDir) && !Directory.Exists(outDir))
                    Directory.CreateDirectory(outDir);

                using var stream = entry.OpenEntryStream();
                using var fileStream = File.Create(outPath);
                stream.CopyTo(fileStream);

                pages.Add(outPath);
            }

            return pages.OrderBy(p => p).ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExtractPages failed: {ex.Message}");
            return new List<string>();
        }
    }

    /// <summary>
    /// 获取网络漫画章节列表
    /// </summary>
    public async Task<List<ComicChapter>> GetChaptersAsync(ComicSource source, string comicUrl)
    {
        var chapters = new List<ComicChapter>();

        try
        {
            var html = await FetchHtmlAsync(comicUrl);
            if (html == null || source.RuleChapters == null) return chapters;

            var doc = await _context.OpenAsync(req => req.Content(html));

            var items = doc.QuerySelectorAll(source.RuleChapters.ChapterList ?? "");
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                var name = item.TextContent?.Trim() ?? $"第 {i + 1} 章";
                var url = item.GetAttribute("href") ?? "";

                if (!string.IsNullOrEmpty(url))
                {
                    chapters.Add(new ComicChapter
                    {
                        Index = i,
                        Title = name,
                        Url = ToAbsoluteUrl(url, source.SourceUrl) ?? ""
                    });
                }
            }
        }
        catch { }

        return chapters;
    }

    /// <summary>
    /// 获取漫画页面图片 URL
    /// </summary>
    public async Task<List<string>> GetPageUrlsAsync(ComicSource source, string chapterUrl)
    {
        var pageUrls = new List<string>();

        try
        {
            var html = await FetchHtmlAsync(chapterUrl);
            if (html == null || source.RulePages == null) return pageUrls;

            var doc = await _context.OpenAsync(req => req.Content(html));

            var images = doc.QuerySelectorAll(source.RulePages.PageList ?? "");
            foreach (var img in images)
            {
                var src = img.GetAttribute(source.RulePages.PageUrl?.Replace("@", "") ?? "src");
                if (!string.IsNullOrEmpty(src))
                {
                    var absoluteUrl = ToAbsoluteUrl(src, source.SourceUrl);
                    if (absoluteUrl != null)
                        pageUrls.Add(absoluteUrl);
                }
            }
        }
        catch { }

        return pageUrls;
    }

    /// <summary>
    /// 保存漫画到数据库
    /// </summary>
    public async Task SaveComicAsync(Comic comic)
    {
        using var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT OR REPLACE INTO Comics
            (Id, Title, Author, FilePath, SourceType, SourceUrl, CoverPath,
             ChapterIndex, PageIndex, AddedTime)
            VALUES
            (@Id, @Title, @Author, @FilePath, @SourceType, @SourceUrl, @CoverPath,
             @ChapterIndex, @PageIndex, @AddedTime)
            """;

        cmd.Parameters.AddWithValue("@Id", comic.Id);
        cmd.Parameters.AddWithValue("@Title", comic.Title);
        cmd.Parameters.AddWithValue("@Author", (object?)comic.Author ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@FilePath", (object?)comic.FilePath ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@SourceType", comic.SourceType);
        cmd.Parameters.AddWithValue("@SourceUrl", (object?)comic.SourceUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CoverPath", (object?)comic.CoverPath ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ChapterIndex", comic.ChapterIndex);
        cmd.Parameters.AddWithValue("@PageIndex", comic.PageIndex);
        cmd.Parameters.AddWithValue("@AddedTime", comic.AddedTime);

        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// 获取所有漫画
    /// </summary>
    public async Task<List<Comic>> GetAllComicsAsync()
    {
        using var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Comics ORDER BY AddedTime DESC";

        var comics = new List<Comic>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            comics.Add(new Comic
            {
                Id = reader.GetString(0),
                Title = reader.GetString(1),
                Author = reader.IsDBNull(2) ? null : reader.GetString(2),
                FilePath = reader.IsDBNull(3) ? null : reader.GetString(3),
                SourceType = reader.GetString(4),
                SourceUrl = reader.IsDBNull(5) ? null : reader.GetString(5),
                CoverPath = reader.IsDBNull(6) ? null : reader.GetString(6),
                ChapterIndex = reader.GetInt32(7),
                PageIndex = reader.GetInt32(8),
                AddedTime = reader.GetString(9)
            });
        }
        return comics;
    }

    private async Task<string?> FetchHtmlAsync(string url)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            return await httpClient.GetStringAsync(url);
        }
        catch
        {
            return null;
        }
    }

    private static string? ToAbsoluteUrl(string? url, string baseUrl)
    {
        if (string.IsNullOrEmpty(url)) return null;
        if (Uri.TryCreate(url, UriKind.Absolute, out _)) return url;
        if (Uri.TryCreate(new Uri(baseUrl), url, out var absoluteUri))
            return absoluteUri.ToString();
        return null;
    }
}

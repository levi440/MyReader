namespace MyReader.Models;

/// <summary>
/// 漫画
/// </summary>
public class Comic
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string? Author { get; set; }
    public string? FilePath { get; set; } // 本地文件路径
    public string SourceType { get; set; } = "local"; // local | network
    public string? SourceUrl { get; set; } // 网络源 URL
    public string? CoverPath { get; set; }
    public int ChapterIndex { get; set; }
    public int PageIndex { get; set; }
    public string AddedTime { get; set; } = DateTime.Now.ToString("O");
}

/// <summary>
/// 漫画源（网络）
/// </summary>
public class ComicSource
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public RuleSearch? RuleSearch { get; set; }
    public RuleChapters? RuleChapters { get; set; }
    public RulePages? RulePages { get; set; }
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// 章节规则
/// </summary>
public class RuleChapters
{
    public string? ChapterList { get; set; }
    public string? ChapterName { get; set; }
    public string? ChapterUrl { get; set; }
}

/// <summary>
/// 页面规则（图片 URL）
/// </summary>
public class RulePages
{
    public string? PageList { get; set; }
    public string? PageUrl { get; set; }
}

/// <summary>
/// 漫画章节
/// </summary>
public class ComicChapter
{
    public int Index { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public List<string> PageUrls { get; set; } = new();
}

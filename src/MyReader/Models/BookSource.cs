namespace MyReader.Models;

/// <summary>
/// 网络书源规则（兼容 Legado 3.0 格式）
/// </summary>
public class BookSource
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string BookSourceUrl { get; set; } = string.Empty;
    public string BookSourceName { get; set; } = string.Empty;
    public string? BookSourceGroup { get; set; }
    public int BookSourceType { get; set; } // 0=小说, 1=漫画
    public string? BookSourceComment { get; set; }
    public string? LoginUrl { get; set; }
    public string? Header { get; set; } // JSON

    // 搜索规则
    public RuleSearch? RuleSearch { get; set; }

    // 书籍信息规则
    public RuleBookInfo? RuleBookInfo { get; set; }

    // 目录规则
    public RuleToc? RuleToc { get; set; }

    // 正文规则
    public RuleContent? RuleContent { get; set; }

    // 发现规则
    public RuleExplore? RuleExplore { get; set; }

    // 配置
    public int Weight { get; set; }
    public int CustomOrder { get; set; }
    public bool Enabled { get; set; } = true;
    public bool EnabledExplore { get; set; }
    public string? ConcurrentRate { get; set; }

    // 元数据
    public string? SearchUrl { get; set; }
    public string? LastUseTime { get; set; }
}

/// <summary>
/// 搜索规则
/// </summary>
public class RuleSearch
{
    public string? BookList { get; set; }
    public string? Name { get; set; }
    public string? Author { get; set; }
    public string? Cover { get; set; }
    public string? Kind { get; set; }
    public string? WordCount { get; set; }
    public string? LastChapter { get; set; }
    public string? Intro { get; set; }
    public string? BookUrl { get; set; }
}

/// <summary>
/// 书籍信息规则
/// </summary>
public class RuleBookInfo
{
    public string? Name { get; set; }
    public string? Author { get; set; }
    public string? Cover { get; set; }
    public string? Kind { get; set; }
    public string? WordCount { get; set; }
    public string? LastChapter { get; set; }
    public string? Intro { get; set; }
    public string? TocUrl { get; set; }
}

/// <summary>
/// 目录规则
/// </summary>
public class RuleToc
{
    public string? ChapterList { get; set; }
    public string? ChapterName { get; set; }
    public string? ChapterUrl { get; set; }
    public string? IsVolume { get; set; }
    public string? UpdateTime { get; set; }
}

/// <summary>
/// 正文规则
/// </summary>
public class RuleContent
{
    public string? Content { get; set; }
    public string? NextContentUrl { get; set; }
    public string? WebJs { get; set; }
    public string? SourceRegex { get; set; }
    public string? ReplaceRegex { get; set; }
}

/// <summary>
/// 发现规则
/// </summary>
public class RuleExplore
{
    public string? BookList { get; set; }
    public string? Name { get; set; }
    public string? Author { get; set; }
    public string? Cover { get; set; }
    public string? BookUrl { get; set; }
}

/// <summary>
/// 搜索结果
/// </summary>
public class SearchResult
{
    public string SourceName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Author { get; set; }
    public string? Cover { get; set; }
    public string? Kind { get; set; }
    public string? LastChapter { get; set; }
    public string? Intro { get; set; }
    public string BookUrl { get; set; } = string.Empty;
}

/// <summary>
/// 章节
/// </summary>
public class ChapterInfo
{
    public int Index { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public bool IsVolume { get; set; }
    public string? UpdateTime { get; set; }
}

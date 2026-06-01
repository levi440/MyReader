namespace MyReader.Models;

/// <summary>
/// RSS/Atom 订阅源
/// </summary>
public class FeedSource
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? SiteUrl { get; set; }
    public string? Icon { get; set; }
    public string? LastFetchTime { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// RSS 文章
/// </summary>
public class FeedArticle
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FeedId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? Content { get; set; }
    public string? Link { get; set; }
    public string? Author { get; set; }
    public string PublishDate { get; set; } = DateTime.Now.ToString("O");
    public bool IsRead { get; set; }
    public bool IsStarred { get; set; }
}

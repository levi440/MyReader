namespace MyReader.Models;

/// <summary>
/// 播客源
/// </summary>
public class PodcastSource
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string RssUrl { get; set; } = string.Empty;
    public string? CoverUrl { get; set; }
    public string? Author { get; set; }
    public string? LastFetchTime { get; set; }
}

/// <summary>
/// 播客单集
/// </summary>
public class PodcastEpisode
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string PodcastId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string AudioUrl { get; set; } = string.Empty;
    public int Duration { get; set; } // 秒
    public string PublishDate { get; set; } = DateTime.Now.ToString("O");
    public bool IsPlayed { get; set; }
    public double PlayPosition { get; set; } // 秒
    public bool IsDownloaded { get; set; }
}

using CodeHollow.FeedReader;
using MyReader.Models;

namespace MyReader.Services;

/// <summary>
/// 播客服务
/// </summary>
public class PodcastService
{
    private readonly DatabaseService _db;

    public PodcastService(DatabaseService db)
    {
        _db = db;
    }

    /// <summary>
    /// 添加播客源
    /// </summary>
    public async Task<PodcastSource?> AddPodcastAsync(string rssUrl)
    {
        try
        {
            var feed = await FeedReader.ReadAsync(rssUrl);

            var podcast = new PodcastSource
            {
                Title = feed.Title ?? rssUrl,
                Description = feed.Description,
                RssUrl = rssUrl,
                CoverUrl = feed.ImageUrl,
                Author = feed.Copyright,
                LastFetchTime = DateTime.Now.ToString("O")
            };

            await SavePodcastAsync(podcast);

            // 获取单集
            foreach (var item in feed.Items)
            {
                var enclosure = item.SpecificItem?.Element?.Element("enclosure");
                var audioUrl = enclosure?.Attribute("url")?.Value;

                if (string.IsNullOrEmpty(audioUrl)) continue;

                var episode = new PodcastEpisode
                {
                    PodcastId = podcast.Id,
                    Title = item.Title ?? "",
                    Description = item.Description,
                    AudioUrl = audioUrl,
                    PublishDate = (item.PublishingDate ?? DateTimeOffset.Now).ToString("O")
                };

                // 尝试解析时长
                var duration = item.SpecificItem?.Element?.Element("{http://www.itunes.com/dtds/podcast-1.0.dtd}duration");
                if (duration != null && int.TryParse(duration.Value, out var seconds))
                {
                    episode.Duration = seconds;
                }

                await SaveEpisodeAsync(episode);
            }

            return podcast;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 刷新播客
    /// </summary>
    public async Task RefreshPodcastAsync(string podcastId)
    {
        var podcast = await GetPodcastAsync(podcastId);
        if (podcast == null) return;

        try
        {
            var feed = await FeedReader.ReadAsync(podcast.RssUrl);

            podcast.Title = feed.Title ?? podcast.Title;
            podcast.Description = feed.Description ?? podcast.Description;
            podcast.CoverUrl = feed.ImageUrl ?? podcast.CoverUrl;
            podcast.LastFetchTime = DateTime.Now.ToString("O");

            await SavePodcastAsync(podcast);

            // 获取已有单集
            var existingEpisodes = await GetEpisodesAsync(podcastId);
            var existingUrls = existingEpisodes.Select(e => e.AudioUrl).ToHashSet();

            foreach (var item in feed.Items)
            {
                var enclosure = item.SpecificItem?.Element?.Element("enclosure");
                var audioUrl = enclosure?.Attribute("url")?.Value;

                if (string.IsNullOrEmpty(audioUrl) || existingUrls.Contains(audioUrl))
                    continue;

                var episode = new PodcastEpisode
                {
                    PodcastId = podcastId,
                    Title = item.Title ?? "",
                    Description = item.Description,
                    AudioUrl = audioUrl,
                    PublishDate = (item.PublishingDate ?? DateTimeOffset.Now).ToString("O")
                };

                await SaveEpisodeAsync(episode);
            }
        }
        catch { }
    }

    /// <summary>
    /// 获取所有播客
    /// </summary>
    public async Task<List<PodcastSource>> GetAllPodcastsAsync()
    {
        using var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Podcasts ORDER BY Title";

        var podcasts = new List<PodcastSource>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            podcasts.Add(new PodcastSource
            {
                Id = reader.GetString(0),
                Title = reader.GetString(1),
                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                RssUrl = reader.GetString(3),
                CoverUrl = reader.IsDBNull(4) ? null : reader.GetString(4),
                Author = reader.IsDBNull(5) ? null : reader.GetString(5),
                LastFetchTime = reader.IsDBNull(6) ? null : reader.GetString(6)
            });
        }
        return podcasts;
    }

    /// <summary>
    /// 获取单个播客
    /// </summary>
    public async Task<PodcastSource?> GetPodcastAsync(string podcastId)
    {
        using var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Podcasts WHERE Id = @Id";
        cmd.Parameters.AddWithValue("@Id", podcastId);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new PodcastSource
            {
                Id = reader.GetString(0),
                Title = reader.GetString(1),
                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                RssUrl = reader.GetString(3),
                CoverUrl = reader.IsDBNull(4) ? null : reader.GetString(4),
                Author = reader.IsDBNull(5) ? null : reader.GetString(5),
                LastFetchTime = reader.IsDBNull(6) ? null : reader.GetString(6)
            };
        }
        return null;
    }

    /// <summary>
    /// 获取播客单集
    /// </summary>
    public async Task<List<PodcastEpisode>> GetEpisodesAsync(string podcastId, int limit = 100)
    {
        using var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Episodes WHERE PodcastId = @PodcastId ORDER BY PublishDate DESC LIMIT @Limit";
        cmd.Parameters.AddWithValue("@PodcastId", podcastId);
        cmd.Parameters.AddWithValue("@Limit", limit);

        var episodes = new List<PodcastEpisode>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            episodes.Add(new PodcastEpisode
            {
                Id = reader.GetString(0),
                PodcastId = reader.GetString(1),
                Title = reader.GetString(2),
                Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                AudioUrl = reader.GetString(4),
                Duration = reader.GetInt32(5),
                PublishDate = reader.GetString(6),
                IsPlayed = reader.GetInt32(7) == 1,
                PlayPosition = reader.GetDouble(8),
                IsDownloaded = reader.GetInt32(9) == 1
            });
        }
        return episodes;
    }

    /// <summary>
    /// 更新播放进度
    /// </summary>
    public async Task UpdatePlayPositionAsync(string episodeId, double position, bool isPlayed = false)
    {
        using var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Episodes SET PlayPosition = @Position, IsPlayed = @IsPlayed WHERE Id = @Id";
        cmd.Parameters.AddWithValue("@Id", episodeId);
        cmd.Parameters.AddWithValue("@Position", position);
        cmd.Parameters.AddWithValue("@IsPlayed", isPlayed ? 1 : 0);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// 删除播客
    /// </summary>
    public async Task DeletePodcastAsync(string podcastId)
    {
        using var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Podcasts WHERE Id = @Id";
        cmd.Parameters.AddWithValue("@Id", podcastId);
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task SavePodcastAsync(PodcastSource podcast)
    {
        using var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT OR REPLACE INTO Podcasts
            (Id, Title, Description, RssUrl, CoverUrl, Author, LastFetchTime)
            VALUES
            (@Id, @Title, @Description, @RssUrl, @CoverUrl, @Author, @LastFetchTime)
            """;

        cmd.Parameters.AddWithValue("@Id", podcast.Id);
        cmd.Parameters.AddWithValue("@Title", podcast.Title);
        cmd.Parameters.AddWithValue("@Description", (object?)podcast.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@RssUrl", podcast.RssUrl);
        cmd.Parameters.AddWithValue("@CoverUrl", (object?)podcast.CoverUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Author", (object?)podcast.Author ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@LastFetchTime", (object?)podcast.LastFetchTime ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync();
    }

    private async Task SaveEpisodeAsync(PodcastEpisode episode)
    {
        using var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT OR REPLACE INTO Episodes
            (Id, PodcastId, Title, Description, AudioUrl, Duration, PublishDate, IsPlayed, PlayPosition, IsDownloaded)
            VALUES
            (@Id, @PodcastId, @Title, @Description, @AudioUrl, @Duration, @PublishDate, @IsPlayed, @PlayPosition, @IsDownloaded)
            """;

        cmd.Parameters.AddWithValue("@Id", episode.Id);
        cmd.Parameters.AddWithValue("@PodcastId", episode.PodcastId);
        cmd.Parameters.AddWithValue("@Title", episode.Title);
        cmd.Parameters.AddWithValue("@Description", (object?)episode.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@AudioUrl", episode.AudioUrl);
        cmd.Parameters.AddWithValue("@Duration", episode.Duration);
        cmd.Parameters.AddWithValue("@PublishDate", episode.PublishDate);
        cmd.Parameters.AddWithValue("@IsPlayed", episode.IsPlayed ? 1 : 0);
        cmd.Parameters.AddWithValue("@PlayPosition", episode.PlayPosition);
        cmd.Parameters.AddWithValue("@IsDownloaded", episode.IsDownloaded ? 1 : 0);

        await cmd.ExecuteNonQueryAsync();
    }
}

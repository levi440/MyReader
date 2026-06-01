using CodeHollow.FeedReader;
using MyReader.Models;

namespace MyReader.Services;

/// <summary>
/// RSS/Atom 订阅服务
/// </summary>
public class FeedService
{
    private readonly DatabaseService _db;

    public FeedService(DatabaseService db)
    {
        _db = db;
    }

    /// <summary>
    /// 添加订阅源
    /// </summary>
    public async Task<FeedSource?> AddFeedAsync(string url)
    {
        try
        {
            var feed = await FeedReader.ReadAsync(url);

            var source = new FeedSource
            {
                Title = feed.Title ?? url,
                Description = feed.Description,
                Url = url,
                SiteUrl = feed.Link,
                LastFetchTime = DateTime.Now.ToString("O")
            };

            await SaveFeedAsync(source);

            // 获取文章
            var articles = feed.Items.Select(item => new FeedArticle
            {
                FeedId = source.Id,
                Title = item.Title ?? "",
                Summary = StripHtml(item.Description ?? ""),
                Content = item.Content ?? item.Description,
                Link = item.Link,
                Author = item.Author,
                PublishDate = (item.PublishingDate ?? DateTimeOffset.Now).ToString("O")
            }).ToList();

            foreach (var article in articles)
            {
                await SaveArticleAsync(article);
            }

            return source;
        }
        catch (Exception ex)
        {
            var source = new FeedSource
            {
                Title = url,
                Url = url,
                ErrorMessage = ex.Message,
                LastFetchTime = DateTime.Now.ToString("O")
            };
            await SaveFeedAsync(source);
            return source;
        }
    }

    /// <summary>
    /// 刷新订阅源
    /// </summary>
    public async Task RefreshFeedAsync(string feedId)
    {
        var feed = await GetFeedAsync(feedId);
        if (feed == null) return;

        try
        {
            var rssFeed = await FeedReader.ReadAsync(feed.Url);

            feed.Title = rssFeed.Title ?? feed.Title;
            feed.Description = rssFeed.Description ?? feed.Description;
            feed.SiteUrl = rssFeed.Link ?? feed.SiteUrl;
            feed.LastFetchTime = DateTime.Now.ToString("O");
            feed.ErrorMessage = null;

            await SaveFeedAsync(feed);

            // 获取新文章
            var existingArticles = await GetArticlesAsync(feedId);
            var existingLinks = existingArticles
                .Where(a => a.Link != null)
                .Select(a => a.Link)
                .ToHashSet();

            foreach (var item in rssFeed.Items)
            {
                if (item.Link != null && existingLinks.Contains(item.Link))
                    continue;

                var article = new FeedArticle
                {
                    FeedId = feedId,
                    Title = item.Title ?? "",
                    Summary = StripHtml(item.Description ?? ""),
                    Content = item.Content ?? item.Description,
                    Link = item.Link,
                    Author = item.Author,
                    PublishDate = (item.PublishingDate ?? DateTimeOffset.Now).ToString("O")
                };

                await SaveArticleAsync(article);
            }
        }
        catch (Exception ex)
        {
            feed.ErrorMessage = ex.Message;
            feed.LastFetchTime = DateTime.Now.ToString("O");
            await SaveFeedAsync(feed);
        }
    }

    /// <summary>
    /// 获取所有订阅源
    /// </summary>
    public async Task<List<FeedSource>> GetAllFeedsAsync()
    {
        using var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Feeds ORDER BY Title";

        var feeds = new List<FeedSource>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            feeds.Add(new FeedSource
            {
                Id = reader.GetString(0),
                Title = reader.GetString(1),
                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                Url = reader.GetString(3),
                SiteUrl = reader.IsDBNull(4) ? null : reader.GetString(4),
                Icon = reader.IsDBNull(5) ? null : reader.GetString(5),
                LastFetchTime = reader.IsDBNull(6) ? null : reader.GetString(6),
                ErrorMessage = reader.IsDBNull(7) ? null : reader.GetString(7)
            });
        }
        return feeds;
    }

    /// <summary>
    /// 获取单个订阅源
    /// </summary>
    public async Task<FeedSource?> GetFeedAsync(string feedId)
    {
        using var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Feeds WHERE Id = @Id";
        cmd.Parameters.AddWithValue("@Id", feedId);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new FeedSource
            {
                Id = reader.GetString(0),
                Title = reader.GetString(1),
                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                Url = reader.GetString(3),
                SiteUrl = reader.IsDBNull(4) ? null : reader.GetString(4),
                Icon = reader.IsDBNull(5) ? null : reader.GetString(5),
                LastFetchTime = reader.IsDBNull(6) ? null : reader.GetString(6),
                ErrorMessage = reader.IsDBNull(7) ? null : reader.GetString(7)
            };
        }
        return null;
    }

    /// <summary>
    /// 获取文章列表
    /// </summary>
    public async Task<List<FeedArticle>> GetArticlesAsync(string feedId, int limit = 100)
    {
        using var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Articles WHERE FeedId = @FeedId ORDER BY PublishDate DESC LIMIT @Limit";
        cmd.Parameters.AddWithValue("@FeedId", feedId);
        cmd.Parameters.AddWithValue("@Limit", limit);

        var articles = new List<FeedArticle>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            articles.Add(new FeedArticle
            {
                Id = reader.GetString(0),
                FeedId = reader.GetString(1),
                Title = reader.GetString(2),
                Summary = reader.IsDBNull(3) ? null : reader.GetString(3),
                Content = reader.IsDBNull(4) ? null : reader.GetString(4),
                Link = reader.IsDBNull(5) ? null : reader.GetString(5),
                Author = reader.IsDBNull(6) ? null : reader.GetString(6),
                PublishDate = reader.GetString(7),
                IsRead = reader.GetInt32(8) == 1,
                IsStarred = reader.GetInt32(9) == 1
            });
        }
        return articles;
    }

    /// <summary>
    /// 获取所有文章
    /// </summary>
    public async Task<List<FeedArticle>> GetAllArticlesAsync(int limit = 200)
    {
        using var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Articles ORDER BY PublishDate DESC LIMIT @Limit";
        cmd.Parameters.AddWithValue("@Limit", limit);

        var articles = new List<FeedArticle>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            articles.Add(new FeedArticle
            {
                Id = reader.GetString(0),
                FeedId = reader.GetString(1),
                Title = reader.GetString(2),
                Summary = reader.IsDBNull(3) ? null : reader.GetString(3),
                Content = reader.IsDBNull(4) ? null : reader.GetString(4),
                Link = reader.IsDBNull(5) ? null : reader.GetString(5),
                Author = reader.IsDBNull(6) ? null : reader.GetString(6),
                PublishDate = reader.GetString(7),
                IsRead = reader.GetInt32(8) == 1,
                IsStarred = reader.GetInt32(9) == 1
            });
        }
        return articles;
    }

    /// <summary>
    /// 标记文章为已读
    /// </summary>
    public async Task MarkAsReadAsync(string articleId)
    {
        using var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Articles SET IsRead = 1 WHERE Id = @Id";
        cmd.Parameters.AddWithValue("@Id", articleId);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// 删除订阅源
    /// </summary>
    public async Task DeleteFeedAsync(string feedId)
    {
        using var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Feeds WHERE Id = @Id";
        cmd.Parameters.AddWithValue("@Id", feedId);
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task SaveFeedAsync(FeedSource feed)
    {
        using var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT OR REPLACE INTO Feeds
            (Id, Title, Description, Url, SiteUrl, Icon, LastFetchTime, ErrorMessage)
            VALUES
            (@Id, @Title, @Description, @Url, @SiteUrl, @Icon, @LastFetchTime, @ErrorMessage)
            """;

        cmd.Parameters.AddWithValue("@Id", feed.Id);
        cmd.Parameters.AddWithValue("@Title", feed.Title);
        cmd.Parameters.AddWithValue("@Description", (object?)feed.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Url", feed.Url);
        cmd.Parameters.AddWithValue("@SiteUrl", (object?)feed.SiteUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Icon", (object?)feed.Icon ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@LastFetchTime", (object?)feed.LastFetchTime ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ErrorMessage", (object?)feed.ErrorMessage ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync();
    }

    private async Task SaveArticleAsync(FeedArticle article)
    {
        using var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT OR REPLACE INTO Articles
            (Id, FeedId, Title, Summary, Content, Link, Author, PublishDate, IsRead, IsStarred)
            VALUES
            (@Id, @FeedId, @Title, @Summary, @Content, @Link, @Author, @PublishDate, @IsRead, @IsStarred)
            """;

        cmd.Parameters.AddWithValue("@Id", article.Id);
        cmd.Parameters.AddWithValue("@FeedId", article.FeedId);
        cmd.Parameters.AddWithValue("@Title", article.Title);
        cmd.Parameters.AddWithValue("@Summary", (object?)article.Summary ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Content", (object?)article.Content ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Link", (object?)article.Link ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Author", (object?)article.Author ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@PublishDate", article.PublishDate);
        cmd.Parameters.AddWithValue("@IsRead", article.IsRead ? 1 : 0);
        cmd.Parameters.AddWithValue("@IsStarred", article.IsStarred ? 1 : 0);

        await cmd.ExecuteNonQueryAsync();
    }

    private static string StripHtml(string html)
    {
        if (string.IsNullOrEmpty(html)) return "";
        var result = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", "");
        return result.Trim();
    }
}

using System.Text.Json;
using MyReader.Models;

namespace MyReader.Services;

/// <summary>
/// 书源导入服务
/// </summary>
public class BookSourceImportService
{
    private readonly DatabaseService _db;
    private readonly HttpClient _httpClient;

    // 内置社区仓库
    private static readonly (string Name, string Url)[] CommunityRepos =
    {
        ("AOAOSTAR 书源", "https://jihulab.com/aoaostar/legado/-/raw/release/cache/3fc2c64c5489c491de6284dca2c2dfce7f551bc9.json"),
        ("XIU2 精品书源", "https://bitbucket.org/xiu2/yuedu/raw/master/shuyuan"),
    };

    public BookSourceImportService(DatabaseService db)
    {
        _db = db;
        _httpClient = new HttpClient();
    }

    /// <summary>
    /// 从社区仓库导入
    /// </summary>
    public async Task<int> ImportFromCommunityRepoAsync(int repoIndex)
    {
        if (repoIndex < 0 || repoIndex >= CommunityRepos.Length)
            return 0;

        var (_, url) = CommunityRepos[repoIndex];
        return await ImportFromUrlAsync(url);
    }

    /// <summary>
    /// 从 URL 导入
    /// </summary>
    public async Task<int> ImportFromUrlAsync(string url)
    {
        try
        {
            var json = await _httpClient.GetStringAsync(url);
            return await ImportFromJsonAsync(json);
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// 从 JSON 字符串导入
    /// </summary>
    public async Task<int> ImportFromJsonAsync(string json)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            // 判断是单源还是数组
            if (json.TrimStart().StartsWith("["))
            {
                var sources = JsonSerializer.Deserialize<List<BookSource>>(json, options);
                if (sources == null) return 0;

                foreach (var source in sources)
                {
                    if (string.IsNullOrEmpty(source.Id))
                        source.Id = Guid.NewGuid().ToString();
                    await SaveBookSourceAsync(source);
                }
                return sources.Count;
            }
            else
            {
                var source = JsonSerializer.Deserialize<BookSource>(json, options);
                if (source == null) return 0;

                if (string.IsNullOrEmpty(source.Id))
                    source.Id = Guid.NewGuid().ToString();
                await SaveBookSourceAsync(source);
                return 1;
            }
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// 保存书源到数据库
    /// </summary>
    public async Task SaveBookSourceAsync(BookSource source)
    {
        using var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT OR REPLACE INTO BookSources
            (Id, BookSourceUrl, BookSourceName, BookSourceGroup, BookSourceType,
             BookSourceComment, LoginUrl, Header, RuleSearch, RuleBookInfo,
             RuleToc, RuleContent, RuleExplore, Weight, CustomOrder,
             Enabled, EnabledExplore, ConcurrentRate, SearchUrl, LastUseTime)
            VALUES
            (@Id, @BookSourceUrl, @BookSourceName, @BookSourceGroup, @BookSourceType,
             @BookSourceComment, @LoginUrl, @Header, @RuleSearch, @RuleBookInfo,
             @RuleToc, @RuleContent, @RuleExplore, @Weight, @CustomOrder,
             @Enabled, @EnabledExplore, @ConcurrentRate, @SearchUrl, @LastUseTime)
            """;

        cmd.Parameters.AddWithValue("@Id", source.Id);
        cmd.Parameters.AddWithValue("@BookSourceUrl", source.BookSourceUrl);
        cmd.Parameters.AddWithValue("@BookSourceName", source.BookSourceName);
        cmd.Parameters.AddWithValue("@BookSourceGroup", (object?)source.BookSourceGroup ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@BookSourceType", source.BookSourceType);
        cmd.Parameters.AddWithValue("@BookSourceComment", (object?)source.BookSourceComment ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@LoginUrl", (object?)source.LoginUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Header", (object?)source.Header ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@RuleSearch", SerializeJson(source.RuleSearch));
        cmd.Parameters.AddWithValue("@RuleBookInfo", SerializeJson(source.RuleBookInfo));
        cmd.Parameters.AddWithValue("@RuleToc", SerializeJson(source.RuleToc));
        cmd.Parameters.AddWithValue("@RuleContent", SerializeJson(source.RuleContent));
        cmd.Parameters.AddWithValue("@RuleExplore", SerializeJson(source.RuleExplore));
        cmd.Parameters.AddWithValue("@Weight", source.Weight);
        cmd.Parameters.AddWithValue("@CustomOrder", source.CustomOrder);
        cmd.Parameters.AddWithValue("@Enabled", source.Enabled ? 1 : 0);
        cmd.Parameters.AddWithValue("@EnabledExplore", source.EnabledExplore ? 1 : 0);
        cmd.Parameters.AddWithValue("@ConcurrentRate", (object?)source.ConcurrentRate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@SearchUrl", (object?)source.SearchUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@LastUseTime", (object?)source.LastUseTime ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// 获取所有书源
    /// </summary>
    public async Task<List<BookSource>> GetAllBookSourcesAsync()
    {
        using var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM BookSources ORDER BY CustomOrder, BookSourceName";

        var sources = new List<BookSource>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            sources.Add(new BookSource
            {
                Id = reader.GetString(0),
                BookSourceUrl = reader.GetString(1),
                BookSourceName = reader.GetString(2),
                BookSourceGroup = reader.IsDBNull(3) ? null : reader.GetString(3),
                BookSourceType = reader.GetInt32(4),
                BookSourceComment = reader.IsDBNull(5) ? null : reader.GetString(5),
                LoginUrl = reader.IsDBNull(6) ? null : reader.GetString(6),
                Header = reader.IsDBNull(7) ? null : reader.GetString(7),
                RuleSearch = DeserializeJson<RuleSearch>(reader.IsDBNull(8) ? null : reader.GetString(8)),
                RuleBookInfo = DeserializeJson<RuleBookInfo>(reader.IsDBNull(9) ? null : reader.GetString(9)),
                RuleToc = DeserializeJson<RuleToc>(reader.IsDBNull(10) ? null : reader.GetString(10)),
                RuleContent = DeserializeJson<RuleContent>(reader.IsDBNull(11) ? null : reader.GetString(11)),
                RuleExplore = DeserializeJson<RuleExplore>(reader.IsDBNull(12) ? null : reader.GetString(12)),
                Weight = reader.GetInt32(13),
                CustomOrder = reader.GetInt32(14),
                Enabled = reader.GetInt32(15) == 1,
                EnabledExplore = reader.GetInt32(16) == 1,
                ConcurrentRate = reader.IsDBNull(17) ? null : reader.GetString(17),
                SearchUrl = reader.IsDBNull(18) ? null : reader.GetString(18),
                LastUseTime = reader.IsDBNull(19) ? null : reader.GetString(19)
            });
        }
        return sources;
    }

    /// <summary>
    /// 获取启用的书源
    /// </summary>
    public async Task<List<BookSource>> GetEnabledBookSourcesAsync()
    {
        var all = await GetAllBookSourcesAsync();
        return all.Where(s => s.Enabled).ToList();
    }

    /// <summary>
    /// 删除书源
    /// </summary>
    public async Task DeleteBookSourceAsync(string id)
    {
        using var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM BookSources WHERE Id = @Id";
        cmd.Parameters.AddWithValue("@Id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// 切换书源启用状态
    /// </summary>
    public async Task ToggleBookSourceAsync(string id, bool enabled)
    {
        using var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE BookSources SET Enabled = @Enabled WHERE Id = @Id";
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.Parameters.AddWithValue("@Enabled", enabled ? 1 : 0);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// 获取社区仓库列表
    /// </summary>
    public static (string Name, string Url)[] GetCommunityRepos()
    {
        return CommunityRepos;
    }

    private static string? SerializeJson<T>(T? obj)
    {
        if (obj == null) return null;
        return JsonSerializer.Serialize(obj);
    }

    private static T? DeserializeJson<T>(string? json) where T : class
    {
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<T>(json);
        }
        catch
        {
            return null;
        }
    }
}

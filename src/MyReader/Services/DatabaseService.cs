using Microsoft.Data.Sqlite;

namespace MyReader.Services;

public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService()
    {
        var dbPath = Path.Combine(AppContext.BaseDirectory, "data", "reader.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        _connectionString = $"Data Source={dbPath}";
        Initialize();
    }

    private void Initialize()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode=WAL;";
        cmd.ExecuteNonQuery();

        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS Books (
                Id TEXT PRIMARY KEY,
                Title TEXT NOT NULL,
                Author TEXT,
                FilePath TEXT NOT NULL,
                FileType TEXT NOT NULL,
                CoverPath TEXT,
                Progress REAL DEFAULT 0,
                LastReadTime TEXT,
                AddedTime TEXT NOT NULL,
                FileSize INTEGER
            );

            CREATE TABLE IF NOT EXISTS Settings (
                Key TEXT PRIMARY KEY,
                Value TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS BookSources (
                Id TEXT PRIMARY KEY,
                BookSourceUrl TEXT NOT NULL,
                BookSourceName TEXT NOT NULL,
                BookSourceGroup TEXT,
                BookSourceType INTEGER DEFAULT 0,
                BookSourceComment TEXT,
                LoginUrl TEXT,
                Header TEXT,
                RuleSearch TEXT,
                RuleBookInfo TEXT,
                RuleToc TEXT,
                RuleContent TEXT,
                RuleExplore TEXT,
                Weight INTEGER DEFAULT 0,
                CustomOrder INTEGER DEFAULT 0,
                Enabled INTEGER DEFAULT 1,
                EnabledExplore INTEGER DEFAULT 0,
                ConcurrentRate TEXT,
                SearchUrl TEXT,
                LastUseTime TEXT
            );

            CREATE TABLE IF NOT EXISTS Comics (
                Id TEXT PRIMARY KEY,
                Title TEXT NOT NULL,
                Author TEXT,
                FilePath TEXT,
                SourceType TEXT NOT NULL DEFAULT 'local',
                SourceUrl TEXT,
                CoverPath TEXT,
                ChapterIndex INTEGER DEFAULT 0,
                PageIndex INTEGER DEFAULT 0,
                AddedTime TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS ComicSources (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                SourceUrl TEXT,
                RuleSearch TEXT,
                RuleChapters TEXT,
                RulePages TEXT,
                Enabled INTEGER DEFAULT 1
            );

            CREATE TABLE IF NOT EXISTS Feeds (
                Id TEXT PRIMARY KEY,
                Title TEXT NOT NULL,
                Description TEXT,
                Url TEXT NOT NULL UNIQUE,
                SiteUrl TEXT,
                Icon TEXT,
                LastFetchTime TEXT,
                ErrorMessage TEXT
            );

            CREATE TABLE IF NOT EXISTS Articles (
                Id TEXT PRIMARY KEY,
                FeedId TEXT NOT NULL,
                Title TEXT NOT NULL,
                Summary TEXT,
                Content TEXT,
                Link TEXT,
                Author TEXT,
                PublishDate TEXT NOT NULL,
                IsRead INTEGER DEFAULT 0,
                IsStarred INTEGER DEFAULT 0,
                FOREIGN KEY (FeedId) REFERENCES Feeds(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS Podcasts (
                Id TEXT PRIMARY KEY,
                Title TEXT NOT NULL,
                Description TEXT,
                RssUrl TEXT NOT NULL UNIQUE,
                CoverUrl TEXT,
                Author TEXT,
                LastFetchTime TEXT
            );

            CREATE TABLE IF NOT EXISTS Episodes (
                Id TEXT PRIMARY KEY,
                PodcastId TEXT NOT NULL,
                Title TEXT NOT NULL,
                Description TEXT,
                AudioUrl TEXT NOT NULL,
                Duration INTEGER,
                PublishDate TEXT NOT NULL,
                IsPlayed INTEGER DEFAULT 0,
                PlayPosition REAL DEFAULT 0,
                IsDownloaded INTEGER DEFAULT 0,
                FOREIGN KEY (PodcastId) REFERENCES Podcasts(Id) ON DELETE CASCADE
            );
            """;
        cmd.ExecuteNonQuery();
    }

    public SqliteConnection GetConnection()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }

    public async Task SaveBookAsync(Models.Book book)
    {
        using var conn = GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT OR REPLACE INTO Books (Id, Title, Author, FilePath, FileType, CoverPath, Progress, LastReadTime, AddedTime, FileSize)
            VALUES (@Id, @Title, @Author, @FilePath, @FileType, @CoverPath, @Progress, @LastReadTime, @AddedTime, @FileSize);
            """;
        cmd.Parameters.AddWithValue("@Id", book.Id);
        cmd.Parameters.AddWithValue("@Title", book.Title);
        cmd.Parameters.AddWithValue("@Author", (object?)book.Author ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@FilePath", book.FilePath);
        cmd.Parameters.AddWithValue("@FileType", book.FileType);
        cmd.Parameters.AddWithValue("@CoverPath", (object?)book.CoverPath ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Progress", book.Progress);
        cmd.Parameters.AddWithValue("@LastReadTime", (object?)book.LastReadTime ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@AddedTime", book.AddedTime);
        cmd.Parameters.AddWithValue("@FileSize", book.FileSize);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<Models.Book>> GetAllBooksAsync()
    {
        using var conn = GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Books ORDER BY LastReadTime DESC;";

        var books = new List<Models.Book>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            books.Add(new Models.Book
            {
                Id = reader.GetString(0),
                Title = reader.GetString(1),
                Author = reader.IsDBNull(2) ? null : reader.GetString(2),
                FilePath = reader.GetString(3),
                FileType = reader.GetString(4),
                CoverPath = reader.IsDBNull(5) ? null : reader.GetString(5),
                Progress = reader.GetDouble(6),
                LastReadTime = reader.IsDBNull(7) ? null : reader.GetString(7),
                AddedTime = reader.GetString(8),
                FileSize = reader.GetInt64(9)
            });
        }
        return books;
    }

    public async Task DeleteBookAsync(string bookId)
    {
        using var conn = GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Books WHERE Id = @Id;";
        cmd.Parameters.AddWithValue("@Id", bookId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateBookProgressAsync(string bookId, double progress)
    {
        using var conn = GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Books SET Progress = @Progress, LastReadTime = @LastReadTime WHERE Id = @Id;";
        cmd.Parameters.AddWithValue("@Id", bookId);
        cmd.Parameters.AddWithValue("@Progress", progress);
        cmd.Parameters.AddWithValue("@LastReadTime", DateTime.Now.ToString("O"));
        await cmd.ExecuteNonQueryAsync();
    }
}

# Windows 阅读器（仿 Legado）从零到开源实施指南

> 技术栈：WinUI 3 + C# + .NET 9 + SQLite + WebView2 + AngleSharp
> 目标：Windows 上的开源阅读器，小说 + 漫画 + RSS + 播客，下载即用
> 数据设计：纯便携，exe 同目录存储，拷贝即迁移
> 参考项目：[Legado / 阅读3.0](https://github.com/gedoor/legado)，[Venera](https://github.com/venera-app/venera)，[小幻阅读](https://reader.richasy.net)

---

## 一、功能优先级

```
☆☆☆☆☆  ① 本地文件导入阅读    EPUB / PDF / TXT / Mobi / FB2
☆☆☆☆☆  ② 漫画阅读          CBZ / CBR / ZIP 本地文件 + 网络漫画源
☆☆☆☆   ③ 网络书源           自定义规则抓取网页小说
☆☆☆    ④ RSS 订阅           标准 RSS / Atom 源
☆☆     ⑤ 播客收听           订阅 + 播放 + 后台 + 下载
☆       ⑥ 设置 / 主题       亮暗主题、字体、备份
```

---

## 二、技术栈选型

### 2.1 核心选型

| 层次 | 技术 | 理由 |
|------|------|------|
| UI 框架 | **WinUI 3** | Windows 11 原生设计，Native AOT 支持 |
| 语言 | **C#** | WinUI 3 官方绑定，生态成熟 |
| 运行时 | **.NET 9** | 最新 LTS，支持 AOT 发布 |
| 存储 | **SQLite**（Microsoft.Data.Sqlite） | 行业标准，事务安全，单文件备份 |
| 文章渲染 | **WebView2** | 系统自带 Edge 内核，原生 WinUI 3 控件 |
| HTML 解析 | **AngleSharp** | 支持 CSS 选择器 + XPath，书源引擎核心 |

### 2.2 完整 NuGet 包清单

```xml
<PackageReference Include="CommunityToolkit.Mvvm" />             <!-- MVVM 框架 -->
<PackageReference Include="Microsoft.Data.Sqlite" />             <!-- SQLite -->
<PackageReference Include="Microsoft.Web.WebView2" />            <!-- 文章/PDF/漫画 渲染 -->
<PackageReference Include="CodeHollow.FeedReader" />             <!-- RSS/Atom/播客 解析 -->
<PackageReference Include="AngleSharp" />                        <!-- 书源规则引擎 -->
<PackageReference Include="VersOne.Epub" />                      <!-- EPUB 解析 -->
<PackageReference Include="PdfSharp" />                          <!-- PDF 文本提取 -->
<PackageReference Include="SharpCompress" />                     <!-- CBZ/CBR/ZIP 漫画解压 -->
```

**就八个。** 没有 ORM，没有 EF Core，没有日志库。

> SharpCompress 同时支持 ZIP、RAR、7z 等格式。CBZ 本质是 ZIP、CBR 本质是 RAR，
> 一个库全部搞定。

### 2.3 开发工具

| 工具 | 用途 |
|------|------|
| **Visual Studio 2022** 社区版 | 安装时勾选 `.NET 桌面开发` + `UWP` 工作负载 |
| **Git + GitHub** | 版本管理 + 开源托管 |
| **Scoop** | 管理工具链（可选） |

### 2.4 环境验证

```bash
dotnet --version          # → 9.x
dotnet new list | findstr "winui"  # → 看到 winui3 模板即就绪
```

---

## 三、项目结构

```
MyReader/
├── MyReader.sln
├── src/MyReader/
│   ├── MyReader.csproj
│   ├── App.xaml / App.cs
│   ├── MainWindow.xaml / .cs
│   │
│   ├── Models/                          # 数据模型
│   │   ├── Book.cs                      # 书籍（本地文件）
│   │   ├── FeedSource.cs                # RSS 源
│   │   ├── FeedArticle.cs               # RSS 文章
│   │   ├── PodcastSource.cs             # 播客源
│   │   ├── PodcastEpisode.cs            # 播客单集
│   │   └── BookSource.cs                # 网络书源规则
│   │
│   ├── Services/                        # 业务逻辑
│   │   ├── DatabaseService.cs           # SQLite 建表 + CRUD
│   │   ├── FileImportService.cs         # 本地文件导入（EPUB/PDF/TXT...）
│   │   ├── BookSourceEngine.cs          # 书源规则引擎（核心）
│   │   ├── FeedService.cs               # RSS 抓取 + 解析
│   │   ├── PodcastService.cs            # 播客抓取 + 解析
│   │   └── AudioPlayerService.cs        # 播放器封装
│   │
│   ├── ViewModels/
│   │   ├── MainViewModel.cs
│   │   ├── LibraryViewModel.cs          # 本地书架
│   │   ├── BookSourceViewModel.cs       # 书源管理
│   │   ├── FeedViewModel.cs
│   │   ├── PodcastViewModel.cs
│   │   └── SettingsViewModel.cs
│   │
│   ├── Views/                           # 页面
│   │   ├── LibraryPage.xaml             # 本地书架
│   │   ├── ReaderPage.xaml              # 阅读器（EPUB/TXT）
│   │   ├── PdfReaderPage.xaml           # PDF 阅读页
│   │   ├── BookSourcePage.xaml          # 书源管理
│   │   ├── FeedPage.xaml                # RSS 订阅
│   │   ├── PodcastPage.xaml             # 播客
│   │   └── SettingsPage.xaml            # 设置
│   │
│   ├── Controls/                        # 自定义控件
│   │   ├── ReaderView.xaml              # 阅读渲染控件
│   │   └── AudioPlayerBar.xaml          # 底部播放栏
│   │
│   ├── Helpers/                         # 工具类
│   │   ├── EpubParser.cs                # EPUB 解析封装
│   │   ├── PdfParser.cs                 # PDF 文本提取
│   │   └── HtmlCleaner.cs               # HTML 净化
│   │
│   └── Assets/
│
├── README.md
├── LICENSE
├── .gitignore
├── screenshots/
└── .github/workflows/build.yml
```

---

## 四、数据层 —— SQLite 完整表设计

> **便携设计**：所有数据存在 exe 同目录的 `data/reader.db`。
> 拷贝整个应用文件夹就意味着完整数据迁移，U 盘、移动硬盘都不会有问题。
> 没有 AppData，无注册表，无隐藏目录。

### 4.1 建表脚本

```sql
-- 本地书籍
CREATE TABLE IF NOT EXISTS Books (
    Id TEXT PRIMARY KEY,
    Title TEXT NOT NULL,
    Author TEXT,
    FilePath TEXT NOT NULL,          -- 本地文件路径
    FileType TEXT NOT NULL,          -- epub/pdf/txt/mobi/fb2
    CoverPath TEXT,                  -- 封面图本地缓存路径
    Progress REAL DEFAULT 0,         -- 阅读进度 0-100
    LastReadTime TEXT,               -- 上次阅读时间
    AddedTime TEXT NOT NULL,         -- 导入时间
    FileSize INTEGER                 -- 文件大小（字节）
);

-- 书签
CREATE TABLE IF NOT EXISTS Bookmarks (
    Id TEXT PRIMARY KEY,
    BookId TEXT NOT NULL,
    ChapterIndex INTEGER,
    ChapterTitle TEXT,
    Position TEXT,                   -- 位置（格式依文件类型而异）
    Note TEXT,
    CreateTime TEXT NOT NULL,
    FOREIGN KEY (BookId) REFERENCES Books(Id) ON DELETE CASCADE
);

-- 网络书源
CREATE TABLE IF NOT EXISTS BookSources (
    Id TEXT PRIMARY KEY,
    Name TEXT NOT NULL,
    BookSourceUrl TEXT,              -- 网站 URL
    RuleSearch TEXT,                 -- 搜索规则（JSON）
    RuleBookInfo TEXT,               -- 书籍信息规则（JSON）
    RuleToc TEXT,                    -- 目录规则（JSON）
    RuleContent TEXT,                -- 正文规则（JSON）
    Enabled INTEGER DEFAULT 1,
    LastUseTime TEXT
);

-- RSS 订阅源
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

-- RSS 文章
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

-- 播客源
CREATE TABLE IF NOT EXISTS Podcasts (
    Id TEXT PRIMARY KEY,
    Title TEXT NOT NULL,
    Description TEXT,
    RssUrl TEXT NOT NULL UNIQUE,
    CoverUrl TEXT,
    Author TEXT,
    LastFetchTime TEXT
);

-- 播客单集
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

-- 设置
CREATE TABLE IF NOT EXISTS Settings (
    Key TEXT PRIMARY KEY,
    Value TEXT NOT NULL
);
```

### 4.2 DatabaseService 骨架

```csharp
public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService()
    {
        // 数据存在 exe 同目录下，拷贝整个文件夹就是完整迁移
        var dbPath = Path.Combine(
            AppContext.BaseDirectory,
            "data", "reader.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        _connectionString = $"Data Source={dbPath}";
        Initialize();
    }

    private void Initialize()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        // 执行上面所有 CREATE TABLE IF NOT EXISTS
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode=WAL;"; // 读写性能优化
        cmd.ExecuteNonQuery();
    }

    public SqliteConnection GetConnection()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }
}
```

---

## 五、分阶段实施

### 第 1 阶段：骨架 + 导航（半天）

- 新建 WinUI 3 项目
- `NavigationView` + `Frame` 页面导航
- 左侧菜单：书架 / 书源 / RSS / 播客 / 设置
- 安装 `CommunityToolkit.Mvvm`
- 搭建 `DatabaseService`，确保启动时建表成功

```xml
<NavigationView PaneDisplayMode="LeftCompact"
                MenuItemInvoked="OnMenuInvoked">
    <NavigationView.MenuItems>
        <NavigationViewItem Content="本地书架" Tag="library" Icon="Library" />
        <NavigationViewItem Content="漫画" Tag="comic" Icon="GenericImage" />
        <NavigationViewItem Content="网络书源" Tag="booksource" Icon="Globe" />
        <NavigationViewItem Content="RSS 订阅" Tag="feed" Icon="Rss" />
        <NavigationViewItem Content="播客" Tag="podcast" Icon="Audio" />
        <NavigationViewItem Content="设置" Tag="settings" Icon="Setting" />
    </NavigationView.MenuItems>
    <Frame x:Name="ContentFrame" />
</NavigationView>
```

---

### 第 2 阶段：本地文件导入 + 阅读（3-4 天）

#### 2.1 文件导入（半天）

```csharp
public class FileImportService
{
    public async Task<Book> ImportFileAsync(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        var fileInfo = new FileInfo(filePath);

        var book = new Book
        {
            Id = Guid.NewGuid().ToString(),
            FilePath = filePath,
            FileType = ext switch
            {
                ".epub" => "epub",
                ".pdf"  => "pdf",
                ".txt"  => "txt",
                ".mobi" => "mobi",
                ".fb2"  => "fb2",
                _ => "unknown"
            },
            Title = Path.GetFileNameWithoutExtension(filePath),
            FileSize = fileInfo.Length,
            AddedTime = DateTime.Now.ToString("O")
        };

        // 根据类型提取元数据
        if (ext == ".epub") await ExtractEpubMetadata(book);
        else if (ext == ".pdf") await ExtractPdfMetadata(book);

        // 存入 SQLite
        await _db.SaveBookAsync(book);
        return book;
    }
}
```

#### 2.2 TXT 阅读（最简单，直接上）

```csharp
// TXT 文件直接读字符串，分章用正则
var text = await File.ReadAllTextAsync(path, Encoding.UTF8);
var chapters = Regex.Split(text, @"(第[一二三四五六七八九十百千]+章\s*.*?[\r\n]+)")
    .Where(s => !string.IsNullOrWhiteSpace(s))
    .ToList();
```

#### 2.3 EPUB 解析

```csharp
using VersOne.Epub;

public async Task<List<EpubChapter>> LoadEpubAsync(string filePath)
{
    var book = await EpubReader.OpenBookAsync(filePath);
    
    // 提取元数据
    var title = book.Title;
    var author = book.Author;
    
    // 提取目录和正文
    var chapters = new List<EpubChapter>();
    foreach (var link in book.TableOfContents)
    {
        var content = await book.ReadCoverAsync(); // 封面
        var html = ReadTextContent(link.HtmlContentFile);
        chapters.Add(new EpubChapter
        {
            Title = link.Title,
            HtmlContent = html
        });
    }
    return chapters;
}
```

渲染 EPUB 正文用 WebView2 展示 HTML：

```csharp
// EPUB 的正文本来就是 HTML，直接丢给 WebView2
ArticleViewer.NavigateToString(epubHtml);
```

#### 2.4 PDF 阅读（见第六章）

#### 2.5 阅读器界面（半天）

- 顶部显示书名 + 章节
- 中间 WebView2 显示内容
- 底部翻页按钮（上一页/下一页）
- 点击中间弹出菜单（目录/设置/书签）
- 字号 + 亮暗 + 滚动模式

---

### 第 3 阶段：网络书源引擎（4-5 天，整个项目最大的模块）

#### 3.1 书源规则格式（完全兼容 Legado）

你的书源要能直接导入 Legado 社区现成的数万个书源 JSON。所以字段名、语法、结构必须和 Legado 3.0 的格式一致。

##### 完整 JSON 结构

```json
{
  "bookSourceUrl": "https://www.biquge.com",
  "bookSourceName": "笔趣阁",
  "bookSourceGroup": "小说",
  "bookSourceType": 0,
  "bookSourceComment": "测试书源",
  "loginUrl": "",
  "header": {
    "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"
  },

  "ruleSearch": {
    "bookList": "div.result-item",
    "name": "h3 a@text",
    "author": "span.author@text",
    "cover": "img.cover@src",
    "kind": "span.category@text",
    "wordCount": "span.wordcount@text",
    "lastChapter": "span.latest@text",
    "intro": "p.desc@text",
    "bookUrl": "h3 a@href"
  },

  "ruleBookInfo": {
    "name": "//h1[@class='book-title']/text()",
    "author": "//p[@class='author']/a/text()",
    "cover": "//div[@class='cover']/img/@src",
    "kind": "//span[@class='kind']/text()",
    "wordCount": "//span[@class='wordcount']/text()",
    "lastChapter": "//a[@class='latest']/text()",
    "intro": "//div[@class='intro']/text()",
    "tocUrl": "//a[@id='toc']/@href"
  },

  "ruleToc": {
    "chapterList": "ul.chapter-list li",
    "chapterName": "a@text",
    "chapterUrl": "a@href",
    "isVolume": "a@class",
    "updateTime": "span.time@text"
  },

  "ruleContent": {
    "content": "div#content@html",
    "nextContentUrl": "a.next@href",
    "webJs": "",
    "sourceRegex": "",
    "replaceRegex": ""
  },

  "ruleExplore": {},
  "ruleReview": null,

  "weight": 0,
  "customOrder": 0,
  "enabled": true,
  "enabledExplore": false,
  "enabledReview": false,
  "enabledGroup": null,
  "concurrentRate": "",

  "searchUrl": null,
  "searchAll": null,
  "bookUrlPattern": null,
  "tocUrl": null,
  "exploreUrl": null,
  "regex": null
}
```

##### 规则语法（与 Legado 完全一致）

提取规则表达式由两部分组成：

```
CSS选择器@属性
```

支持的 `@属性`：

| 语法 | 含义 | 示例 |
|------|------|------|
| `@text` | 元素的纯文本 | `h1@text` → "第一章 穿越" |
| `@html` | 元素的 innerHTML | `div#content@html` → `"<p>正文...</p>"` |
| `@textNodes` | 所有文本节点拼接 | `div@textNodes` |
| `@href` | 链接 | `a@href` → `/book/123.html` |
| `@src` | 图片地址 | `img@src` → `https://.../cover.jpg` |
| `@alt` | 图片 alt 文本 | `img@alt` |
| `@title` | 标题属性 | `a@title` |
| `@class` | class 属性 | `div@class` |
| `@id` | id 属性 | `div@id` |

也支持 **XPath**（直接写 XPath 表达式，不含 `@` 时自动识别）：

```
//h1[@class='book-title']/text()
//div[@id='content']/p[1]/text()
```

##### 规则目录各字段说明

| ruleSearch 字段 | 含义 |
|----------------|------|
| `bookList` | 搜索结果列表中每条结果的容器元素 |
| `name` | 书名 |
| `author` | 作者 |
| `cover` | 封面图 URL |
| `kind` | 分类 |
| `wordCount` | 字数 |
| `lastChapter` | 最新章节名 |
| `intro` | 简介 |
| `bookUrl` | 书籍详情页 URL |

| ruleBookInfo 字段 | 含义 |
|------------------|------|
| `name` | 书名 |
| `author` | 作者 |
| `cover` | 封面 URL |
| `kind` | 分类 |
| `wordCount` | 字数 |
| `lastChapter` | 最新章节名 |
| `intro` | 简介 |
| `tocUrl` | 目录页 URL（如果和详情页不同） |

| ruleToc 字段 | 含义 |
|-------------|------|
| `chapterList` | 章节列表的每条容器 |
| `chapterName` | 章节名 |
| `chapterUrl` | 章节链接 |
| `isVolume` | 是否为卷（用于分卷的小说） |
| `updateTime` | 章节更新时间 |

| ruleContent 字段 | 含义 |
|-----------------|------|
| `content` | 正文内容容器 |
| `nextContentUrl` | 下一页链接（分页时用） |
| `webJs` | 加载后执行的 JS（用于动态页面） |
| `sourceRegex` | 源文本正则过滤 |
| `replaceRegex` | 正文替换规则（去除广告等） |

#### 3.2 规则引擎实现

```csharp
public class BookSourceEngine
{
    private readonly IBrowsingContext _context;

    public BookSourceEngine()
    {
        var config = Configuration.Default
            .WithCss()
            .WithXPath()
            .WithDefaultLoader();
        _context = BrowsingContext.New(config);
    }

    /// <summary>搜索书籍（并发查询所有启用的书源）</summary>
    public async Task<List<SearchResult>> SearchAllAsync(
        List<BookSource> sources, string keyword)
    {
        var tasks = sources
            .Where(s => s.Enabled)
            .Select(s => SearchSingleAsync(s, keyword));

        var results = await Task.WhenAll(tasks);
        return results.Where(r => r != null)
                      .SelectMany(r => r)
                      .ToList();
    }

    private async Task<List<SearchResult>?> SearchSingleAsync(
        BookSource source, string keyword)
    {
        try
        {
            var url = string.Format(source.SearchUrl ??
                source.BookSourceUrl + "/search?q={0}",
                Uri.EscapeDataString(keyword));

            var doc = await _context.OpenAsync(url);
            var rules = source.RuleSearch;
            var items = doc.QuerySelectorAll(rules.BookList);

            return items.Select(item => new SearchResult
            {
                SourceName = source.BookSourceName,
                Name = ExtractByRule(item, rules.Name),
                Author = ExtractByRule(item, rules.Author),
                Cover = ExtractByRule(item, rules.Cover),
                Kind = ExtractByRule(item, rules.Kind),
                LastChapter = ExtractByRule(item, rules.LastChapter),
                Intro = ExtractByRule(item, rules.Intro),
                BookUrl = ToAbsoluteUrl(
                    ExtractByRule(item, rules.BookUrl),
                    source.BookSourceUrl)
            }).ToList();
        }
        catch
        {
            return null; // 一个书源挂了不影响其他
        }
    }

    /// <summary>获取目录</summary>
    public async Task<List<Chapter>> GetTocAsync(BookSource source, string bookUrl)
    {
        var doc = await _context.OpenAsync(bookUrl);
        var rules = source.RuleToc;

        var items = doc.QuerySelectorAll(rules.ChapterList);
        return items.Select((item, index) => new Chapter
        {
            Index = index,
            Title = ExtractByRule(item, rules.ChapterName),
            Url = ToAbsoluteUrl(
                ExtractByRule(item, rules.ChapterUrl),
                source.BookSourceUrl)
        }).ToList();
    }

    /// <summary>获取正文</summary>
    public async Task<string> GetContentAsync(BookSource source, string chapterUrl)
    {
        var doc = await _context.OpenAsync(chapterUrl);
        var html = ExtractByRule(doc, source.RuleContent.Content);

        // 应用替换规则（去广告等）
        if (!string.IsNullOrEmpty(source.RuleContent.ReplaceRegex))
        {
            // 支持多行替换规则，每行格式: 正则@@替换文本
            foreach (var line in source.RuleContent.ReplaceRegex.Split('\n'))
            {
                var parts = line.Split("@@");
                if (parts.Length == 2)
                    html = Regex.Replace(html, parts[0], parts[1]);
            }
        }

        return html;
    }

    /// <summary>根据规则表达式提取内容</summary>
    private string ExtractByRule(IParentNode element, string? rule)
    {
        if (string.IsNullOrEmpty(rule)) return "";

        if (rule.Contains('@'))
        {
            // CSS 选择器语法：div.title@text
            var parts = rule.Split('@', 2);
            var selector = parts[0];
            var attr = parts[1];
            var el = element.QuerySelector(selector);
            if (el == null) return "";

            return attr switch
            {
                "text" => el.TextContent?.Trim() ?? "",
                "html" => el.InnerHtml?.Trim() ?? "",
                "textNodes" => string.Concat(el.ChildNodes
                    .OfType<IText>()
                    .Select(n => n.Text)).Trim(),
                _ => el.GetAttribute(attr)?.Trim() ?? ""
            };
        }
        else
        {
            // XPath
            var navigator = element.CreateNavigator();
            var result = navigator?.Evaluate(rule);
            return result?.ToString()?.Trim() ?? "";
        }
    }

    private string ToAbsoluteUrl(string? url, string baseUrl)
    {
        if (string.IsNullOrEmpty(url)) return "";
        if (Uri.TryCreate(url, UriKind.Absolute, out _)) return url;
        var base = new Uri(baseUrl);
        return new Uri(base, url).ToString();
    }
}
```

#### 3.3 书源管理 —— 导入、缓存、兜底

##### 3.3.1 总体设计

软件的核心理念：**枪弹分离**。应用本身不带任何源，源的获取完全独立。

四种导入方式，覆盖所有场景：

```
┌──────────────────────────────────────────────┐
│  书源管理                         [+ 添加]   │
│                                              │
│  ┌─ 添加书源 ─────────────────────────────┐  │
│  │                                        │  │
│  │  ○ 社区仓库    从在线仓库一键拉取        │  │
│  │  ○ URL 导入    粘贴 JSON 链接            │  │
│  │  ○ 本地导入    选择 .json 文件           │  │
│  │  ○ 手动编写    F12 照着网站写规则        │  │
│  │                                        │  │
│  └────────────────────────────────────────┘  │
│                                              │
│  已安装的源：                                 │
│  ┌─────────────────────────────────────────┐ │
│  │ ✅ 笔趣阁         0个源有问题        设为启用  │ │
│  │ ✅ 69书吧         0个源有问题        设为启用  │ │
│  │ ❌ 全本小说网      0个源有问题        设为禁用  │ │
│  └─────────────────────────────────────────┘ │
└──────────────────────────────────────────────┘
```

##### 3.3.2 方式一：社区仓库（最省事）

内置多个社区维护的源仓库地址，用户点一下就行：

```csharp
// 内置仓库列表
private static readonly (string Name, string Url)[] CommunityRepos =
{
    ("AOAOSTAR 书源",
     "https://jihulab.com/aoaostar/legado/-/raw/release/cache/3fc2c64c5489c491de6284dca2c2dfce7f551bc9.json"),
    ("XIU2 精品书源",
     "https://bitbucket.org/xiu2/yuedu/raw/master/shuyuan"),
    ("一程书源",
     "https://www.gitlink.org.cn/api/yi-c/yd/raw/sy.json?ref=master"),
    // 你可以在 GitHub Release 上自己维护一份镜像，
    // 作为最后的兜底仓库
};

// 一键导入
public async Task ImportFromCommunityRepoAsync(int repoIndex)
{
    var (_, url) = CommunityRepos[repoIndex];
    await ImportFromUrlAsync(url);
}
```

UI 上就是一个简单的下拉选择 + 按钮：

```
社区仓库：
┌──────────────────────────────┐
│ [AOAOSTAR 书源  ▼]  [导入]  │
└──────────────────────────────┘
```

##### 3.3.3 方式二：URL 导入

用户粘贴一个 JSON 链接，自动识别是单源还是仓库：

```csharp
public async Task ImportFromUrlAsync(string url)
{
    var json = await _httpClient.GetStringAsync(url);

    // 自动缓存原始 JSON 到本地，防仓库日后失效
    await CacheSourceJsonAsync(url, json);

    // 判断是单源还是仓库
    if (json.TrimStart().StartsWith("["))
    {
        var sources = JsonSerializer.Deserialize<List<BookSource>>(json);
        foreach (var s in sources)
        {
            s.Id = Guid.NewGuid().ToString();
            await _db.SaveBookSourceAsync(s);
        }
        ShowToast($"导入了 {sources.Count} 个书源");
    }
    else
    {
        var source = JsonSerializer.Deserialize<BookSource>(json);
        source.Id = Guid.NewGuid().ToString();
        await _db.SaveBookSourceAsync(source);
        ShowToast($"已导入：{source.BookSourceName}");
    }
}
```

##### 3.3.4 自动本地缓存

每次从 URL 导入源，同时把原始 JSON 存一份到本地。
未来在线仓库挂了，用户可以直接从本地缓存恢复：

```csharp
public async Task CacheSourceJsonAsync(string url, string json)
{
    var cacheDir = Path.Combine(
        AppContext.BaseDirectory, "data", "source-cache");
    Directory.CreateDirectory(cacheDir);

    var fileName = $"{DateTime.Now:yyyy-MM-dd_HHmm}_{new Uri(url).Host}.json";
    var cachePath = Path.Combine(cacheDir, fileName);
    await File.WriteAllTextAsync(cachePath, json);
}

// 从缓存恢复
public async Task ImportFromCacheAsync(string cacheFile)
{
    var json = await File.ReadAllTextAsync(cacheFile);
    await ImportFromJsonAsync(json);
}
```

文件结构：

```
data/
├── reader.db
└── source-cache/
    ├── 2026-05-30_1430_jihulab.com.json     ← 上次从社区仓库拉的
    ├── 2026-05-27_biquge.json               ← 手动导入的单源
    └── 2026-05-31_bitbucket.org.json         ← 昨天又拉了一次
```

##### 3.3.5 手动编写源（F12 工作流）

如果所有仓库都没人维护了，且没有本地缓存，也没有朋友分享的 JSON。
最后的兜底方案：**只要目标网站还开着，你就能自己写源。**

工作流如下：

```
1. 浏览器打开目标小说网站
2. F12 打开开发者工具
3. 搜索一本小说 → 在 Elements 面板里找到搜索结果的 HTML 结构
4. 记录 CSS 选择器，填入 bookList、name、author、bookUrl
5. 点进详情页 → 找到目录链接和书籍信息的选择器
6. 点进目录页 → 找到章节列表的选择器
7. 点进正文页 → 找到正文内容的选择器
8. 点"测试" → 验证能否正常搜出结果、获取正文
```

软件内置一个"测试源"功能，写完规则点测试，直接显示搜出来什么：

```
测试源：笔趣阁

搜索关键词：斗破苍穹

搜索成功！找到 3 条结果：
┌────────────────────────────────────┐
│ 📖 斗破苍穹                         │
│    作者：天蚕土豆                    │
│    链接：/book/123                  │
│                                    │
│ 📖 斗破苍穹之无上之境                │
│    作者：xxx                        │
│    链接：/book/456                  │
└────────────────────────────────────┘

[获取正文测试]
点击目录 → 找到 120 章 → 获取第一章正文 ✓
正文内容预览：第一章 陨落的天才...
```

写一个源十分钟，加上测试验证十分钟。单个源的时间成本就这么多。

##### 3.3.6 兜底完整链

```
在线仓库可用？
  ├─ 是 → 一键导入，自动缓存，顺便用
  └─ 否 → 有本地缓存？
            ├─ 是 → 从缓存恢复
            └─ 否 → 有朋友分享的 JSON？
                      ├─ 是 → 本地导入
                      └─ 否 → 目标网站能打开？
                                ├─ 是 → F12 自己写一个，十分钟
                                └─ 否 → 无解，换别的站
```

只要最底层（网站）还在，这个链就不会断。而网站会不会倒闭，不是你的软件能控制的，也不是你需要控制的。

##### 3.3.7 源管理其他功能

- **启用/禁用**：禁用某个源后不参与聚合搜索
- **删除源**：偶尔清理没用的源
- **导出选中源**：选几个源导出为 JSON，发给朋友。P2P 分享不依赖任何在线服务
- **定期检查**：启动时对所有源发一个轻量请求，标记出已失效的源

---

### 第 3.5 阶段：漫画模块（2-3 天）

#### 本地漫画（CBR/CBZ/ZIP）

CBZ = ZIP 包了一堆图片，CBR = RAR。用 SharpCompress 一个库解压全部：

```csharp
public List<string> ExtractPages(string filePath)
{
    var tempDir = Path.Combine(
        Path.GetTempPath(), "MyReader", Guid.NewGuid().ToString());
    Directory.CreateDirectory(tempDir);

    using var stream = File.OpenRead(filePath);
    using var archive = ArchiveFactory.Open(stream);

    var pages = new List<string>();
    foreach (var entry in archive.Entries)
    {
        if (entry.IsDirectory) continue;
        var ext = Path.GetExtension(entry.Key).ToLowerInvariant();
        if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp")) continue;

        var outPath = Path.Combine(tempDir, entry.Key);
        entry.WriteToFile(outPath);
        pages.Add(outPath);
    }
    return pages.OrderBy(p => p).ToList();
}
```

#### 网络漫画源

漫画源跟书源的核心区别：书源提取 **HTML 文本**→ WebView2 渲染，漫画源提取 **图片 URL 列表** → 图片控件翻页。

规则格式（CSS/XPath + @语法风格一致，但不兼容 Legado，因为它不是漫画软件）：

```json
{
  "name": "漫画源名",
  "sourceUrl": "https://...",
  "ruleSearch": {
    "comicList": "div.comic-item",
    "name": "h3@text", "author": "span@text",
    "cover": "img@src", "url": "a@href"
  },
  "ruleChapters": {
    "chapterList": "ul li",
    "chapterName": "a@text",
    "chapterUrl": "a@href"
  },
  "rulePages": {
    "pageList": "div.image-list img",
    "pageUrl": "@src"
  }
}
```

```csharp
// 核心差异：提取一话的所有图片 URL
public async Task<List<string>> GetPageUrlsAsync(
    ComicSource source, string chapterUrl)
{
    var doc = await _context.OpenAsync(chapterUrl);
    var images = doc.QuerySelectorAll(source.RulePages.PageList);
    return images
        .Select(img => ExtractByRule(img, source.RulePages.PageUrl))
        .Select(url => ToAbsoluteUrl(url, source.SourceUrl))
        .Where(url => !string.IsNullOrEmpty(url))
        .ToList();
}
```

#### 漫画阅读 UI

- **单页模式**：一屏一页，左右键翻页
- **连续滚动**：所有页面垂直排列，鼠标滚轮上下滑（手机看漫画的体验）
- **双页模式**：适用于横屏显示器，两页并排
- 阅读时解压到临时目录，退出时清理
- 最后一页点"下一话"自动加载并继续

#### SQLite 新表

```sql
CREATE TABLE IF NOT EXISTS Comics (
    Id TEXT PRIMARY KEY,
    Title TEXT NOT NULL,
    Author TEXT,
    FilePath TEXT,
    SourceType TEXT NOT NULL DEFAULT 'local',  -- 'local' | 'network'
    CoverPath TEXT,
    ChapterIndex INTEGER DEFAULT 0,
    PageIndex INTEGER DEFAULT 0,
    AddedTime TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS ComicSources (
    Id TEXT PRIMARY KEY,
    Name TEXT NOT NULL,
    SourceUrl TEXT,
    RuleSearch TEXT,        -- JSON
    RuleChapters TEXT,       -- JSON
    RulePages TEXT,          -- JSON
    Enabled INTEGER DEFAULT 1
);
```

---

### 第 4 阶段：RSS 订阅（1 天）

```csharp
public async Task<List<FeedArticle>> FetchFeedAsync(string url)
{
    var feed = await FeedReader.ReadAsync(url);
    return feed.Items.Select(item => new FeedArticle
    {
        Title = item.Title,
        Summary = StripHtml(item.Description ?? ""),
        Content = item.Content ?? item.Description,
        Link = item.Link,
        PublishDate = item.PublishingDate ?? DateTimeOffset.Now
    }).ToList();
}
```

- 订阅管理页面：添加/删除/刷新
- 文章列表页：标题 + 摘要 + 时间 + 已读状态
- 阅读页：WebView2 渲染（同 EPUB 阅读页面可复用）

---

### 第 5 阶段：播客播放（1-2 天）

```csharp
public class AudioPlayerService
{
    private readonly MediaPlayer _player = new();

    public void Play(string audioUrl)
    {
        _player.Source = MediaSource.CreateFromUri(new Uri(audioUrl));
        _player.Play();

        // 系统媒体控件（锁屏、蓝牙耳机控制）
        var smtc = SystemMediaTransportControls.GetForCurrentView();
        smtc.IsPlayEnabled = true;
        smtc.IsPauseEnabled = true;
        smtc.PlaybackStatus = MediaPlaybackStatus.Playing;
    }

    public void SetSpeed(double speed) => _player.PlaybackRate = speed;
}
```

- 播客解析复用 RSS 的 FeedReader（播客也是 RSS + enclosure）
- 底部常驻播放栏
- 下载离线收听
- 倍速 0.5x - 3x

---

### 第 6 阶段：设置 + 开源准备（半天）

- 亮色/暗色主题
- 字体大小
- 数据备份：数据库在 exe 同目录的 `data/reader.db`，备份直接复制文件夹。
  迁移到新电脑：整个文件夹拷过去就行
- WebDav 备份（可选，云端自动备份）
- 书源导入/导出（JSON 文件）
- 订阅列表导出（OPML / JSON）
- README + LICENSE（MIT）+ .gitignore + CI

---

## 六、关键难点

### 6.1 PDF 渲染 —— 三种方案对比

| 方案 | 优点 | 缺点 | 推荐度 |
|------|------|------|--------|
| **WebView2 + PDF.js** | 渲染效果最好，支持页码/缩放/搜索 | 需要内嵌 PDF.js 库（约 2MB） | ⭐⭐⭐ 推荐 |
| **PdfiumViewer** | 原生控件，轻量 | 界面简陋，定制困难，维护不活跃 | ⭐⭐ |
| **系统默认打开** | 零代码 | 脱离应用，体验分裂 | ⭐ |

**推荐方案：WebView2 + PDF.js**

```xml
<!-- 在 ReaderPage.xaml 里复用 WebView2 -->
<WebView2 x:Name="PdfViewer" />
```

```csharp
// 用 PDF.js 渲染
public void LoadPdf(string pdfPath)
{
    // 方案 A：将 PDF 转为 Base64 嵌入 HTML
    var pdfBytes = File.ReadAllBytes(pdfPath);
    var base64 = Convert.ToBase64String(pdfBytes);
    
    var html = $@"
    <!DOCTYPE html>
    <html>
    <head>
        <script src='ms-appx-web:///Assets/pdf.min.js'></script>
        <style>
            body {{ margin: 0; background: #525659; }}
            canvas {{ display: block; margin: 0 auto; }}
        </style>
    </head>
    <body>
        <div id='viewer'></div>
        <script>
            pdfjsLib.getDocument({{ data: '{base64}' }}).promise
                .then(pdf => {{
                    for (let i = 1; i <= pdf.numPages; i++) {{
                        pdf.getPage(i).then(page => {{
                            var scale = 1.5;
                            var viewport = page.getViewport({{ scale }});
                            var canvas = document.createElement('canvas');
                            canvas.width = viewport.width;
                            canvas.height = viewport.height;
                            document.getElementById('viewer').appendChild(canvas);
                            page.render({{
                                canvasContext: canvas.getContext('2d'),
                                viewport: viewport
                            }});
                        }});
                    }}
                }});
        </script>
    </body>
    </html>";

    PdfViewer.NavigateToString(html);
}
```

> **注意**：如果只想提取 PDF 文本（不渲染页面），可以用 `PdfSharp` 或 `iTextSharp` 直接读文本内容。

### 6.2 书源规则引擎的整体架构

```
用户操作                     引擎逻辑                   网络请求
┌────────────┐    ┌─────────────────────┐    ┌──────────────┐
│ 输入关键词  │───▶│ SearchAsync()       │───▶│ HTTP GET     │
└────────────┘    │  CSS/XPath 解析      │    │ 目标网站     │
                  │  返回搜索结果列表    │    └──────────────┘
┌────────────┐    └─────────────────────┘
│ 选择书籍    │───▶   T o c A s y n c ( )    │───▶│ HTTP GET     │
└────────────┘    │  获取目录章节列表    │    └──────────────┘
                  └─────────────────────┘
┌────────────┐    ┌─────────────────────┐    ┌──────────────┐
│ 点击章节    │───▶│ GetContentAsync()   │───▶│ HTTP GET     │
└────────────┘    │  提取正文 HTML      │    └──────────────┘
                  │  HTML 净化 + 翻页   │
                  │  WebView2 渲染      │
                  └─────────────────────┘
```

核心要点：

- **书源规则**存 SQLite，用户可导入导出 JSON。格式完全兼容 Legado 3.0，
  社区现有的数万条书源可以直接导入使用
- **CSS 选择器 + XPath** 两种语法，通过 `@` 分隔选择器和属性
- **并发请求**：搜索时对所有已启用的书源并发请求，合并结果
- **失败隔离**：某个书源挂了不影响其他书源
- **请求频率控制**：加一个简单的限流，避免被目标网站封 IP
- **书源市场**：用户可以导出自己的书源 JSON 分享给朋友，
  也可以从 Legado 社区（如 yckceo.com、legado.aoaostar.com）下载现成的书源导入

### 6.3 各文件类型的阅读流程

```
                              ┌──────────────┐
                              │ 打开阅读器页面 │
                              └──────┬───────┘
                                     │
                    ┌────────────────┼────────────────┐
                    ▼                ▼                ▼
              ┌──────────┐    ┌──────────┐    ┌──────────┐
              │   TXT    │    │   EPUB   │    │   PDF    │
              └────┬─────┘    └────┬─────┘    └────┬─────┘
                   ▼               ▼               ▼
              ┌──────────┐    ┌──────────┐    ┌──────────┐
              │ 文本分章  │    │ EPUB解析 │    │ PDF.js   │
              │ RichText  │    │ HTML提取 │    │ 逐页渲染  │
              │ Block渲染 │    │WebView2  │    │WebView2  │
              └──────────┘    └──────────┘    └──────────┘
                                     │
                              ┌──────┴──────┐
                              │  网络书源    │
                              │ (也是 HTML)  │
                              │  WebView2   │
                              └─────────────┘

              ┌──────────┐    ┌──────────┐
              │  本地漫画  │    │  网络漫画源 │
              │ CBZ/CBR  │    │ (图片URL)  │
              └────┬─────┘    └────┬─────┘
                   ▼               ▼
              ┌──────────┐    ┌──────────┐
              │ 解压到临时 │    │ 规则引擎  │
              │ 目录排序  │    │ 提取URLs  │
              └────┬─────┘    └────┬─────┘
                   ▼               ▼
              ┌──────────────────────┐
              │   FlipView / 滚动    │
              │   图片控件逐页显示    │
              │   退出时清理临时文件  │
              └──────────────────────┘
```

**三种渲染出口：** TXT → RichTextBlock。HTML（EPUB/书源/RSS/PDF）→ WebView2。
漫画/UIImage → 原生 Image 控件 + FlipView 翻页。

### 6.4 导入/导出分享

### 6.4 导入 / 导出 —— P2P 分发

#### 书源导出

```csharp
// 书源导出为 JSON（发给朋友），不依赖任何在线服务
public string ExportBookSources(List<string>? sourceIds = null)
{
    var sources = sourceIds == null
        ? _db.GetAllBookSources()
        : _db.GetBookSourcesByIds(sourceIds);

    return JsonSerializer.Serialize(sources, new JsonSerializerOptions
    {
        WriteIndented = true
    });
    // 导出后：文件 → 发给朋友 → 他选"本地导入" → 完事
    // 这条链不需要网络，U盘拷过去都行
}
```

#### 订阅列表导出

```csharp
// 订阅列表导出为 OPML（标准 RSS 交换格式）
public string ExportFeedsAsOpml()
{
    var feeds = _db.GetAllFeeds();
    var opml = new XDocument(
        new XElement("opml", new XAttribute("version", "2.0"),
            new XElement("head",
                new XElement("title", "MyReader Subscriptions")),
            new XElement("body",
                feeds.Select(f => new XElement("outline",
                    new XAttribute("type", "rss"),
                    new XAttribute("text", f.Title),
                    new XAttribute("xmlUrl", f.Url)))))
    );
    return opml.ToString();
}
```

#### 分发链路

```
在线仓库挂了 → 本地缓存恢复
缓存也没有 → 朋友导出 JSON 发你 → 本地导入
朋友也没有 → 目标网站还开着 → F12 自己写一个
```

软件不需要依赖任何在线服务，源 JSON 的传输完全走 P2P。

---

## 七、时间预估

| 阶段 | 内容 | 时间 |
|------|------|------|
| 1 | 骨架 + 导航 + SQLite 初始化 | 半天 |
| 2 | 本地文件导入 + TXT/EPUB 阅读 | 3 天 |
| 3 | PDF 渲染方案落地 | 1 天 |
| 4 | 网络书源规则引擎 | 4-5 天 |
| 5 | 漫画模块（本地解压 + 网络源 + 阅读器） | 2-3 天 |
| 6 | RSS 订阅 | 1 天 |
| 7 | 播客播放器 | 1-2 天 |
| 8 | 设置 + 主题 + WebDav 备份 | 1 天 |
| 9 | GitHub 开源准备 | 半天 |
| **总计** | | **约 14-17 天** |

> 网络书源引擎 + 漫画模块约占一半的工作量。其他都有现成 NuGet 包。

---

## 八、GitHub 开源准备

### 仓库结构

```
MyReader/
├── src/MyReader/
├── README.md
├── LICENSE                 ← MIT
├── .gitignore              ← bin/ obj/ .vs/ publish/ data/
├── screenshots/
└── .github/workflows/build.yml
```

### CI 示例（自动编译验证）

```yaml
name: Build
on: [push, pull_request]
jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.x'
      - run: dotnet restore
      - run: dotnet build --configuration Release --no-restore
```

### README 要点

- 功能截图（至少 3 张：书架/阅读器/书源管理）
- 安装方式（下载 Release 解压运行）
- 书源 / 订阅源导入方式说明
- 构建指南
- 参考致谢（Legado、Venera、小幻阅读）
- 许可证 MIT

---

## 九、学习参考资源

| 资源 | 链接 |
|------|------|
| WinUI 3 官方文档 | https://learn.microsoft.com/windows/apps/winui/ |
| CommunityToolkit.Mvvm | https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/ |
| AngleSharp 文档 | https://anglesharp.github.io/ |
| VersOne.Epub | https://github.com/vers-one/EpubReader |
| CodeHollow.FeedReader | https://github.com/codehollow/FeedReader |
| PDF.js | https://mozilla.github.io/pdf.js/ |
| WinUI 3 示例 | https://github.com/microsoft/WinUI-Gallery |
| Legado（参考） | https://github.com/gedoor/legado |
| Venera（参考） | https://github.com/venera-app/venera |
| 小幻阅读（参考） | https://github.com/Richasy/ReaderCopilot.Public |
| 小幻阅读文档 | https://reader.richasy.net/zh/docs/ |

### 社区书源仓库

| 仓库 | 说明 |
|------|------|
| AOAOSTAR | https://legado.aoaostar.com/ — Legado 书源仓库（最活跃） |
| XIU2 精品书源 | https://yuedu.xiu2.xyz/ — 精选高质量源 |
| yckceo 源仓库 | https://www.yckceo.com/ — 综合源仓库 |
| legado 官方论坛 | https://legado.cn/ — 讨论 + 源求助 |
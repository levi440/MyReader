using MyReader.Models;
using VersOne.Epub;

namespace MyReader.Services;

public class FileImportService
{
    private readonly DatabaseService _db;

    public FileImportService(DatabaseService db)
    {
        _db = db;
    }

    public async Task<ImportResult> ImportFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
            return ImportResult.Fail("文件不存在");

        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        var fileInfo = new FileInfo(filePath);

        var book = new Book
        {
            FilePath = filePath,
            FileType = ext switch
            {
                ".epub" => "epub",
                ".pdf" => "pdf",
                ".txt" => "txt",
                ".mobi" => "mobi",
                ".fb2" => "fb2",
                _ => "unknown"
            },
            Title = Path.GetFileNameWithoutExtension(filePath),
            FileSize = fileInfo.Length,
            AddedTime = DateTime.Now.ToString("O")
        };

        if (book.FileType == "unknown")
            return ImportResult.Fail("不支持的文件格式");

        // 提取元数据
        if (ext == ".epub")
        {
            var epubResult = await ExtractEpubMetadata(book, filePath);
            if (!epubResult.Success)
                return epubResult;
        }

        await _db.SaveBookAsync(book);
        return ImportResult.Ok(book);
    }

    private async Task<ImportResult> ExtractEpubMetadata(Book book, string filePath)
    {
        try
        {
            var epubBook = await EpubReader.OpenBookAsync(filePath);

            if (!string.IsNullOrEmpty(epubBook.Title))
                book.Title = epubBook.Title;

            book.Author = epubBook.AuthorList?.FirstOrDefault();

            // 提取封面
            var cover = await epubBook.ReadCoverAsync();
            if (cover != null && cover.Length > 0)
            {
                var coverDir = Path.Combine(AppContext.BaseDirectory, "data", "covers");
                Directory.CreateDirectory(coverDir);
                var coverPath = Path.Combine(coverDir, $"{book.Id}.jpg");
                await File.WriteAllBytesAsync(coverPath, cover);
                book.CoverPath = coverPath;
            }

            return ImportResult.Ok(book);
        }
        catch (Exception ex)
        {
            return ImportResult.Fail($"EPUB 解析失败：{ex.Message}");
        }
    }
}

namespace MyReader.Models;

public class Book
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string? Author { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty; // epub/pdf/txt/mobi/fb2
    public string? CoverPath { get; set; }
    public double Progress { get; set; } // 0-100
    public string? LastReadTime { get; set; }
    public string AddedTime { get; set; } = DateTime.Now.ToString("O");
    public long FileSize { get; set; }
}

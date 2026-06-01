namespace MyReader.Models;

/// <summary>
/// 导入结果
/// </summary>
public class ImportResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Book? Book { get; set; }

    public static ImportResult Ok(Book book) => new() { Success = true, Book = book };
    public static ImportResult Fail(string error) => new() { Success = false, ErrorMessage = error };
}

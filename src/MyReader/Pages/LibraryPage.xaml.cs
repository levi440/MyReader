using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MyReader.Models;
using MyReader.Services;
using Windows.Storage.Pickers;

namespace MyReader.Pages;

public sealed partial class LibraryPage : Page
{
    private readonly FileImportService _importService;
    private List<Book> _books = new();
    private Book? _selectedBook;

    public LibraryPage()
    {
        InitializeComponent();
        _importService = new FileImportService(App.Database);
        Loaded += LibraryPage_Loaded;
    }

    private async void LibraryPage_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadBooks();
    }

    private async Task LoadBooks()
    {
        _books = await App.Database.GetAllBooksAsync();
        BookGrid.ItemsSource = _books;

        EmptyState.Visibility = _books.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        BookGrid.Visibility = _books.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void AddBook_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        picker.FileTypeFilter.Add(".epub");
        picker.FileTypeFilter.Add(".pdf");
        picker.FileTypeFilter.Add(".txt");

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var files = await picker.PickMultipleFilesAsync();
        if (files != null && files.Count > 0)
        {
            var errors = new List<string>();
            foreach (var file in files)
            {
                var result = await _importService.ImportFileAsync(file.Path);
                if (!result.Success && result.ErrorMessage != null)
                {
                    errors.Add($"{file.Name}: {result.ErrorMessage}");
                }
            }

            if (errors.Count > 0)
            {
                var dialog = new ContentDialog
                {
                    Title = "导入完成",
                    Content = string.Join("\n", errors),
                    CloseButtonText = "确定",
                    XamlRoot = XamlRoot
                };
                await dialog.ShowAsync();
            }

            await LoadBooks();
        }
    }

    private void BookGrid_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is Book book)
        {
            Frame.Navigate(typeof(ReaderPage), book);
        }
    }

    private void BookGrid_RightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs e)
    {
        // 获取右键点击的元素
        if (e.OriginalSource is FrameworkElement element)
        {
            // 尝试从元素或其父元素获取 Book 数据
            var book = FindBookFromElement(element);
            if (book != null)
            {
                _selectedBook = book;
            }
        }
    }

    /// <summary>
    /// 从元素向上查找 Book 数据
    /// </summary>
    private Book? FindBookFromElement(FrameworkElement element)
    {
        var current = element;
        while (current != null)
        {
            if (current.DataContext is Book book)
                return book;
            current = current.Parent as FrameworkElement;
        }
        return null;
    }

    private void OpenBook_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedBook != null)
        {
            Frame.Navigate(typeof(ReaderPage), _selectedBook);
        }
    }

    private async void DeleteBook_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedBook == null)
        {
            var noBookDialog = new ContentDialog
            {
                Title = "提示",
                Content = "请先右键点击要删除的书籍",
                CloseButtonText = "确定",
                XamlRoot = XamlRoot
            };
            await noBookDialog.ShowAsync();
            return;
        }

        var bookToDelete = _selectedBook;

        var dialog = new ContentDialog
        {
            Title = "从书架移除",
            Content = $"确定要从书架中移除《{bookToDelete.Title}》吗？\n\n原始文件不会被删除。",
            PrimaryButtonText = "移除",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            // 只删除数据库记录，不删除磁盘文件
            await App.Database.DeleteBookAsync(bookToDelete.Id);
            _selectedBook = null;
            await LoadBooks();
        }
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await LoadBooks();
    }
}

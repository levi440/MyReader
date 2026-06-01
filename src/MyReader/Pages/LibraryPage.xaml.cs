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
            foreach (var file in files)
            {
                await _importService.ImportFileAsync(file.Path);
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

    private void BookGrid_ContextRequested(UIElement sender, Microsoft.UI.Xaml.Input.ContextRequestedEventArgs args)
    {
        if (args.OriginalSource is FrameworkElement element && element.DataContext is Book book)
        {
            _selectedBook = book;
        }
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
        if (_selectedBook == null) return;

        var dialog = new ContentDialog
        {
            Title = "删除书籍",
            Content = $"确定要删除《{_selectedBook.Title}》吗？",
            PrimaryButtonText = "删除",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Close
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await App.Database.DeleteBookAsync(_selectedBook.Id);
            _selectedBook = null;
            await LoadBooks();
        }
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await LoadBooks();
    }
}

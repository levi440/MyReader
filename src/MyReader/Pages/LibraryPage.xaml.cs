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

        // WinUI 3 需要设置窗口句柄
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
}

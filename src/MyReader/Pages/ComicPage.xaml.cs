using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MyReader.Models;
using MyReader.Services;
using Windows.Storage.Pickers;

namespace MyReader.Pages;

public sealed partial class ComicPage : Page
{
    private readonly ComicService _comicService;
    private List<Comic> _comics = new();

    public ComicPage()
    {
        InitializeComponent();
        _comicService = new ComicService(App.Database);
        Loaded += ComicPage_Loaded;
    }

    private async void ComicPage_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadComics();
    }

    private async Task LoadComics()
    {
        _comics = await _comicService.GetAllComicsAsync();
        ComicGrid.ItemsSource = _comics;

        EmptyState.Visibility = _comics.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        ComicGrid.Visibility = _comics.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void AddComic_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        picker.FileTypeFilter.Add(".cbz");
        picker.FileTypeFilter.Add(".cbr");
        picker.FileTypeFilter.Add(".zip");

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.Current);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var files = await picker.PickMultipleFilesAsync();
        if (files != null && files.Count > 0)
        {
            foreach (var file in files)
            {
                await _comicService.ImportLocalComicAsync(file.Path);
            }
            await LoadComics();
        }
    }

    private void ComicGrid_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is Comic comic)
        {
            // TODO: 打开漫画阅读器
            Frame.Navigate(typeof(ReaderPage), comic);
        }
    }
}

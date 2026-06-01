using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using MyReader.Models;
using MyReader.Services;

namespace MyReader.Pages;

public sealed partial class ComicReaderPage : Page
{
    private Comic? _comic;
    private ComicService? _comicService;
    private List<string> _pagePaths = new();
    private int _currentPage = 0;

    public ComicReaderPage()
    {
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is Comic comic)
        {
            _comic = comic;
            _comicService = new ComicService(App.Database);
            TitleBlock.Text = comic.Title;

            await LoadComicContent();
        }
    }

    private async Task LoadComicContent()
    {
        if (_comic == null || _comicService == null) return;

        try
        {
            if (_comic.SourceType == "local" && !string.IsNullOrEmpty(_comic.FilePath))
            {
                if (!File.Exists(_comic.FilePath))
                {
                    ShowError("漫画文件不存在");
                    return;
                }

                _pagePaths = _comicService.ExtractPages(_comic.FilePath);
                if (_pagePaths.Count > 0)
                {
                    _currentPage = _comic.PageIndex;
                    if (_currentPage >= _pagePaths.Count) _currentPage = 0;
                    ShowPage(_currentPage);
                }
                else
                {
                    ShowError("漫画文件中没有找到图片");
                }
            }
            else
            {
                ShowError("暂不支持网络漫画");
            }
        }
        catch (Exception ex)
        {
            ShowError($"加载失败：{ex.Message}");
        }
    }

    private void ShowPage(int pageIndex)
    {
        if (pageIndex < 0 || pageIndex >= _pagePaths.Count) return;

        _currentPage = pageIndex;
        var filePath = _pagePaths[pageIndex];

        var bitmap = new BitmapImage(new Uri(filePath));
        ComicImage.Source = bitmap;

        PageInfo.Text = $"{pageIndex + 1} / {_pagePaths.Count}";
        PageSlider.Maximum = _pagePaths.Count - 1;
        PageSlider.Value = pageIndex;

        PrevPageButton.IsEnabled = pageIndex > 0;
        NextPageButton.IsEnabled = pageIndex < _pagePaths.Count - 1;
    }

    private void ShowError(string message)
    {
        PageInfo.Text = message;
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        SaveProgress();
        if (Frame.CanGoBack)
            Frame.GoBack();
    }

    private void PrevPage_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPage > 0)
        {
            ShowPage(_currentPage - 1);
        }
    }

    private void NextPage_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPage < _pagePaths.Count - 1)
        {
            ShowPage(_currentPage + 1);
        }
    }

    private void PageSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        var newPage = (int)e.NewValue;
        if (newPage != _currentPage && newPage >= 0 && newPage < _pagePaths.Count)
        {
            ShowPage(newPage);
        }
    }

    private async void SaveProgress()
    {
        if (_comic == null) return;
        _comic.PageIndex = _currentPage;
        await _comicService?.SaveComicAsync(_comic)!;
    }
}

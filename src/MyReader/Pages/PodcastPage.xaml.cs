using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MyReader.Models;
using MyReader.Services;

namespace MyReader.Pages;

public sealed partial class PodcastPage : Page
{
    private readonly PodcastService _podcastService;
    private PodcastSource? _selectedPodcast;

    public PodcastPage()
    {
        InitializeComponent();
        _podcastService = new PodcastService(App.Database);
        Loaded += PodcastPage_Loaded;
    }

    private async void PodcastPage_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadPodcasts();
    }

    private async Task LoadPodcasts()
    {
        var podcasts = await _podcastService.GetAllPodcastsAsync();
        PodcastList.ItemsSource = podcasts;

        PodcastEmptyState.Visibility = podcasts.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        PodcastList.Visibility = podcasts.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void PodcastList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PodcastList.SelectedItem is PodcastSource podcast)
        {
            _selectedPodcast = podcast;
            EpisodeTitle.Text = podcast.Title;
            await LoadEpisodes(podcast.Id);
        }
    }

    private async Task LoadEpisodes(string podcastId)
    {
        var episodes = await _podcastService.GetEpisodesAsync(podcastId);
        EpisodeList.ItemsSource = episodes;

        EpisodeEmptyState.Visibility = episodes.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        EpisodeList.Visibility = episodes.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void PodcastList_ContextRequested(UIElement sender, Microsoft.UI.Xaml.Input.ContextRequestedEventArgs args)
    {
        if (args.OriginalSource is FrameworkElement element && element.DataContext is PodcastSource podcast)
        {
            _selectedPodcast = podcast;
        }
    }

    private async void AddPodcast_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "添加播客",
            PrimaryButtonText = "添加",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary
        };

        var urlBox = new TextBox
        {
            PlaceholderText = "输入播客 RSS 源 URL",
            MinWidth = 300
        };
        dialog.Content = urlBox;

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && !string.IsNullOrEmpty(urlBox.Text))
        {
            var podcast = await _podcastService.AddPodcastAsync(urlBox.Text);
            if (podcast != null)
            {
                await LoadPodcasts();
                PodcastList.SelectedItem = podcast;
            }
        }
    }

    private async void RefreshPodcast_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedPodcast == null) return;

        await _podcastService.RefreshPodcastAsync(_selectedPodcast.Id);
        await LoadEpisodes(_selectedPodcast.Id);
    }

    private async void DeletePodcast_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedPodcast == null) return;

        var dialog = new ContentDialog
        {
            Title = "删除播客",
            Content = $"确定要删除《{_selectedPodcast.Title}》吗？",
            PrimaryButtonText = "删除",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Close
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await _podcastService.DeletePodcastAsync(_selectedPodcast.Id);
            _selectedPodcast = null;
            await LoadPodcasts();
            EpisodeList.ItemsSource = null;
        }
    }

    private void EpisodeList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is PodcastEpisode episode)
        {
            // TODO: 播放音频
        }
    }
}

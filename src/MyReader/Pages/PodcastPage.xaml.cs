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

    private void EpisodeList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is PodcastEpisode episode)
        {
            // TODO: 播放音频
        }
    }
}

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MyReader.Models;
using MyReader.Services;

namespace MyReader.Pages;

public sealed partial class PodcastPage : Page
{
    private readonly PodcastService _podcastService;
    private readonly AudioPlayerService _audioPlayer;
    private PodcastSource? _selectedPodcast;
    private PodcastEpisode? _currentEpisode;

    public PodcastPage()
    {
        InitializeComponent();
        _podcastService = new PodcastService(App.Database);
        _audioPlayer = new AudioPlayerService();
        _audioPlayer.PlaybackStarted += AudioPlayer_PlaybackStarted;
        _audioPlayer.PlaybackStopped += AudioPlayer_PlaybackStopped;
        _audioPlayer.ErrorOccurred += AudioPlayer_ErrorOccurred;
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
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
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
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
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

    private async void EpisodeList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is PodcastEpisode episode)
        {
            _currentEpisode = episode;

            // 标记为已播放
            await _podcastService.UpdatePlayPositionAsync(episode.Id, 0, true);

            // 播放音频
            if (!string.IsNullOrEmpty(episode.AudioUrl))
            {
                var confirmDialog = new ContentDialog
                {
                    Title = "播放播客",
                    Content = $"要播放《{episode.Title}》吗？\n\n将使用系统默认播放器打开。",
                    PrimaryButtonText = "播放",
                    CloseButtonText = "取消",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = XamlRoot
                };

                var result = await confirmDialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    _audioPlayer.Play(episode.AudioUrl);
                }
            }
            else
            {
                var errorDialog = new ContentDialog
                {
                    Title = "播放失败",
                    Content = "该单集没有音频链接",
                    CloseButtonText = "确定",
                    XamlRoot = XamlRoot
                };
                await errorDialog.ShowAsync();
            }
        }
    }

    private void AudioPlayer_PlaybackStarted(object? sender, EventArgs e)
    {
    }

    private void AudioPlayer_PlaybackStopped(object? sender, EventArgs e)
    {
    }

    private async void AudioPlayer_ErrorOccurred(object? sender, string error)
    {
        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
        {
            var dialog = new ContentDialog
            {
                Title = "播放错误",
                Content = error,
                CloseButtonText = "确定",
                XamlRoot = XamlRoot
            };
            await dialog.ShowAsync();
        });
    }
}

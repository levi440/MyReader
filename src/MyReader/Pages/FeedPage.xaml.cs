using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MyReader.Models;
using MyReader.Services;

namespace MyReader.Pages;

public sealed partial class FeedPage : Page
{
    private readonly FeedService _feedService;
    private FeedSource? _selectedFeed;

    public FeedPage()
    {
        InitializeComponent();
        _feedService = new FeedService(App.Database);
        Loaded += FeedPage_Loaded;
    }

    private async void FeedPage_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadFeeds();
    }

    private async Task LoadFeeds()
    {
        var feeds = await _feedService.GetAllFeedsAsync();
        FeedList.ItemsSource = feeds;

        FeedEmptyState.Visibility = feeds.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        FeedList.Visibility = feeds.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void FeedList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FeedList.SelectedItem is FeedSource feed)
        {
            _selectedFeed = feed;
            ArticleTitle.Text = feed.Title;
            await LoadArticles(feed.Id);
        }
    }

    private async Task LoadArticles(string feedId)
    {
        var articles = await _feedService.GetArticlesAsync(feedId);
        ArticleList.ItemsSource = articles;

        ArticleEmptyState.Visibility = articles.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        ArticleList.Visibility = articles.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void AddFeed_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "添加 RSS 订阅",
            PrimaryButtonText = "添加",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary
        };

        var urlBox = new TextBox
        {
            PlaceholderText = "输入 RSS/Atom 源 URL",
            MinWidth = 300
        };
        dialog.Content = urlBox;

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && !string.IsNullOrEmpty(urlBox.Text))
        {
            var feed = await _feedService.AddFeedAsync(urlBox.Text);
            if (feed != null)
            {
                await LoadFeeds();
                FeedList.SelectedItem = feed;
            }
        }
    }

    private async void ArticleList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is FeedArticle article)
        {
            // 标记为已读
            await _feedService.MarkAsReadAsync(article.Id);

            // 打开文章
            if (!string.IsNullOrEmpty(article.Link))
            {
                await Windows.System.Launcher.LaunchUriAsync(new Uri(article.Link));
            }
        }
    }
}

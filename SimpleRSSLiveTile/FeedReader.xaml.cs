using HtmlAgilityPack;
using RSSDataTypes.Data;
using SimpleRSSLiveTile.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Syndication;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace SimpleRSSLiveTile
{
    /// <summary>
    /// Simple Class for representing a Feed's articles.
    /// </summary>
    public class Article
    {
        private Uri goodUri;

        public string Title { get; set; }
        public Uri URL { get; set; }
        public string Summary { get; set; }
        public DateTimeOffset PublishedDate { get; set; }
        public string PublishedDateFormatted => PublishedDate.ToString("dd/MM/yyyy    h:mm tt").ToUpper();

        public Article()
        {
            Title = "Feed";
            URL = new Uri("");
            PublishedDate = new DateTimeOffset(DateTime.Now);
        }

        public Article(ISyndicationText title, ISyndicationText summary, DateTimeOffset publishedDate, Uri baseUri)
        {
            if (title != null)
                Title = title.Text;
            if (summary != null)
                Summary = summary.Text;

            PublishedDate = publishedDate;
            URL = baseUri;
        }

    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FeedReader : Page
    {

        private static DependencyProperty s_itemProperty
            = DependencyProperty.Register("Feed", typeof(FeedViewModel), typeof(FeedDetailPage), new PropertyMetadata(null));

        private FeedDataSource feedDB = new FeedDataSource();

        public static DependencyProperty ItemProperty
        {
            get { return s_itemProperty; }
        }

        public FeedViewModel Feed
        {
            get { return (FeedViewModel)GetValue(s_itemProperty); }
            set { SetValue(s_itemProperty, value); }
        }

        public FeedReader()
        {
            this.InitializeComponent();
        }

        private async void PageRoot_Loaded(object sender, RoutedEventArgs e)
        {
            // Realize the main page content.
            FindName("RootPanel");
            RootPanel.Visibility = Visibility.Collapsed;

            feedTitle.Text = Feed.Title;
            String hiResFavicon = await feedDB.GetFeedById(Feed.Id).getHiResFaviconAsync();
            feedHQFavicon.Source = new BitmapImage(new Uri(hiResFavicon, UriKind.Absolute));

            FeedWaiting.Visibility = Visibility.Collapsed;
            RootPanel.Visibility = Visibility.Visible;


        }


        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Parameter is feed ID
            Feed f = feedDB.GetFeedById(int.Parse((String)e.Parameter));
            Feed = FeedViewModel.FromFeed(f);

            //Start an update for the tile while we're at it
            f.UpdateTileAsync();

            //We build the feed's article list
            SyndicationFeed feedData = await f.GetFeedDataAsync();

            // Grab feed items and add them to the tiles.
            foreach (var item in feedData.Items)
            {
                //Try getting a valid URL for the item.
                Uri goodUri = item.ItemUri ?? item.Links.Select(l => l.Uri).FirstOrDefault();

                Article a = new Article(item.Title, item.Summary, item.PublishedDate, goodUri);
 
                //Strip all XML/HTML tags.
                HtmlDocument doc = new HtmlDocument();
                if (a.Title != null)
                {
                    doc.LoadHtml(a.Title);
                    a.Title = doc.DocumentNode.InnerText;
                }

                if (a.Summary != null)
                {
                    doc.LoadHtml(a.Summary);
                    a.Summary = doc.DocumentNode.InnerText;
                }

                Feed.Articles.Add(a);
            }

            if (CoreApplication.GetCurrentView() != CoreApplication.MainView)
                MainAppButton.Visibility = Visibility.Collapsed;

        }

        private void MainAppButton_Click(object sender, RoutedEventArgs e)
        {
            //If the window is a subwindow, just close it, the main app is still open. Otherwise, navigate to the main page.
            if (CoreApplication.GetCurrentView() == CoreApplication.MainView)
                Frame.Navigate(typeof(NewMainPage));
        }

        private async void OpenArticle(object sender, ItemClickEventArgs e)
        {
            Article selectedArticle = (Article)e.ClickedItem;
            await Windows.System.Launcher.LaunchUriAsync(selectedArticle.URL);
        }
    }
}

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
using Windows.UI;
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

            //Ensure the titlebar is consistent with the rest of the app 
            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
            ApplicationViewTitleBar titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = (Color)Application.Current.Resources["SystemAccentColor"];
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            titleBar.ButtonForegroundColor = (Color)Application.Current.Resources["SystemAltHighColor"];

            // Realize the main page content.
            FindName("RootPanel");
            RootPanel.Visibility = Visibility.Collapsed;

            feedTitle.Text = Feed.Title;
            String hiResFavicon = await feedDB.GetFeedById(Feed.Id).getHiResFaviconAsync();
            feedHQFavicon.Source = new BitmapImage(new Uri(hiResFavicon, UriKind.Absolute));

            FeedWaiting.Visibility = Visibility.Collapsed;
            RootPanel.Visibility = Visibility.Visible;

            if (CoreApplication.GetCurrentView() == CoreApplication.MainView)
                MainAppButton.Visibility = Visibility.Visible;

        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Parameter is feed ID
            Feed f = feedDB.GetFeedById(int.Parse((String)e.Parameter));
            Feed = FeedViewModel.FromFeed(f);

            //Try to update feed articles
            await f.UpdateFeedArticlesAsync();

            //Start an update for the tile while we're at it
            await f.UpdateTileAsync();

            //We build the feedVM's article list from the cached feed articles
            IList<Article> art = await feedDB.GetCachedArticles(f);
            art.ToList().ForEach( i => Feed.Articles.Add(i));

            if (Feed.Articles.Count == 0)
            {
                //No articles, display error message
                FeedWaiting.Visibility = Visibility.Collapsed;
                FeedBroken.Visibility = Visibility.Visible;

            }

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

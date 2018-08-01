using SimpleRSSLiveTile.ViewModels;
using RSSDataTypes.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using System.Threading.Tasks;
using System.Xml;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Popups;
using Windows.ApplicationModel.Core;
using Windows.UI.ViewManagement;

namespace SimpleRSSLiveTile
{

    public sealed partial class FeedDetailPage : Page
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

        public FeedDetailPage()
        {
            this.InitializeComponent();
        }

        //Parses the fields of the page and builds matching Feed item.
        private async Task<Feed> BuildFeedFromPage()
        {
            String tileXML = customTileXMLContent.Text;

            //Use default XML if there's nothing in the textbox
            if (tileXML== "")
            {
                ResourceLoader rl = new ResourceLoader();
                tileXML = rl.GetString("AdaptiveTemplate");
            }
            
            Feed feedToSave = new Feed(Feed.Id, "New RSS Feed", feedInput.Text, tileXML);

            feedToSave.SetAtomIconUse(atomIconToggle.IsOn);
            String feedTitle= await feedToSave.GetFeedTitleAsync();

            feedToSave.SetTitle(feedTitle);

            return feedToSave;

        }

        //Build a feed object with all the data we have, check if the XML and URL are correct, and save it to the DB.
        //This doesn't pin the feed to the start menu.
        private async void SaveFeed(object sender, RoutedEventArgs e)
        {
            Progress.Visibility = Visibility.Visible;
            
            //Test Feed.
            Feed f = await BuildFeedFromPage();

            //Test XML.
            Boolean validXML = f.TestTileXML();
            if (!validXML)
            {
                outputStackPanel.Visibility = Visibility.Visible;
                symbolOutput.Foreground = new SolidColorBrush(Windows.UI.Colors.Red);
                symbolOutput.Symbol = Symbol.Cancel;
                greetingOutput.Foreground = new SolidColorBrush(Windows.UI.Colors.Red);
                greetingOutput.Text = "Invalid XML.";
                Progress.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                return;
            }
            
            if (f.IsTileValid())
            {
                //We're good, save the feed and enable pinning.
                feedDB.SetFeed(f);
        
                symbolOutput.Foreground = new SolidColorBrush(Windows.UI.Colors.Green);
                symbolOutput.Symbol = Symbol.Accept;
          
                greetingOutput.Foreground = new SolidColorBrush(Windows.UI.Colors.Green);

                //Update feed Title and favicon
                feedTitle.Text = f.GetTitle();
                String faviconURL = await f.getHiResFaviconAsync();
                var bitmapImage = new BitmapImage();
                feedHQFavicon.Source = bitmapImage;
                bitmapImage.UriSource = new Uri(faviconURL, UriKind.Absolute);

                //Maybe we're saving a tile already pinned, in which case we'll just update it
                if (feedDB.GetFeedById(f.GetId()).IsTilePinned()) 
                {
                    greetingOutput.Text = "Feed Saved ! Live Tile will be automatically updated.";
                    unpinButton.Visibility = Visibility.Visible;
                    await f.UpdateTileAsync();
                }
                else
                {
                    greetingOutput.Text = "Feed Saved ! You can now pin it.";
                    pinButton.Visibility = Visibility.Visible;
                }
       
                Progress.Visibility = Visibility.Collapsed;
                outputStackPanel.Visibility = Visibility.Visible;
                readButton.Visibility = Visibility.Visible;

                //Update list in main page
                GlobalMainPageReference.mainPage.UpdateFeedList();
            }
            else
            {
                outputStackPanel.Visibility = Visibility.Visible;
                symbolOutput.Symbol = Symbol.Cancel;
                symbolOutput.Foreground = new SolidColorBrush(Windows.UI.Colors.Red);
                greetingOutput.Foreground = new SolidColorBrush(Windows.UI.Colors.Red);
                greetingOutput.Text = "Invalid RSS Feed.";
                Progress.Visibility = Visibility.Collapsed;
            }
        }

        private async void PinFeed(object sender, RoutedEventArgs e)
        {
            Feed f = await BuildFeedFromPage();
            Progress.Visibility = Visibility.Visible;
            
            //Check if the feed built from the page is valid.
            //If not, ask the user to save his feed.
            if (f.IsTileValid())
            {
                await f.PinTileAsync();

                //Save pinned feed state
                feedDB.SetFeed(f);

                if (f.IsTilePinned())
                {
                    outputStackPanel.Visibility = Visibility.Visible;
                    symbolOutput.Symbol = Symbol.Accept;
                    symbolOutput.Foreground = new SolidColorBrush(Windows.UI.Colors.Green);
                    greetingOutput.Foreground = new SolidColorBrush(Windows.UI.Colors.Green);
                    greetingOutput.Text = "Feed pinned !"; 
                    unpinButton.Visibility = Visibility.Visible;
                    pinButton.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                outputStackPanel.Visibility = Visibility.Visible;
                symbolOutput.Symbol = Symbol.Cancel;
                symbolOutput.Foreground = new SolidColorBrush(Windows.UI.Colors.Red);
                greetingOutput.Foreground = new SolidColorBrush(Windows.UI.Colors.Red);
                greetingOutput.Text = "Please save the modified feed before trying to pin it.";
                pinButton.Visibility = Visibility.Collapsed;
            }

            Progress.Visibility = Visibility.Collapsed;
        }

        private async void UnpinFeed(object sender, RoutedEventArgs e)
        {
            Feed f = await BuildFeedFromPage();
            Progress.Visibility = Visibility.Visible;
           
            f.UnpinTileAsync();

            //Save unpinned feed state
            feedDB.SetFeed(f);

            if (!f.IsTilePinned())
            {
                outputStackPanel.Visibility = Visibility.Visible;
                symbolOutput.Symbol = Symbol.Accept;
                symbolOutput.Foreground = new SolidColorBrush(Windows.UI.Colors.Green);
                greetingOutput.Foreground = new SolidColorBrush(Windows.UI.Colors.Green);
                greetingOutput.Text = "Feed unpinned.";
                unpinButton.Visibility = Visibility.Collapsed;

                if (f.IsTileValid())
                    pinButton.Visibility = Visibility.Visible;
            }

            Progress.Visibility = Visibility.Collapsed;
        }

        private void ToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            ToggleSwitch toggleSwitch = sender as ToggleSwitch;
            if (toggleSwitch != null)
            {
                if (toggleSwitch.IsOn == true)
                {
                    //We set the XML this way since linebreaks fuck up data binding
                    customTileXMLContent.Text = Feed.TileXML;
                    customTileXML.Visibility = Visibility.Visible;
                }
                else
                {
                    customTileXML.Visibility = Visibility.Collapsed;
                    customTileXMLContent.Text = "";
                }
            }
        }

        private void Show_FormattingHelp(object sender, TappedRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
        }


        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Parameter is feed ID
            FeedViewModel viewModel = (FeedViewModel)e.Parameter;
            Feed = FeedViewModel.FromFeed(feedDB.GetFeedById(viewModel.Id));

        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
        }


        private async void PageRoot_Loaded(object sender, RoutedEventArgs e)
        {
            CreatePageContent();
        }

        private async void CreatePageContent()
        {
            // Realize the main page content.
            FindName("RootPanel");
            RootPanel.Visibility = Visibility.Collapsed;

            //Update the viewmodel based on the new data in the database, and refresh the UI elements
            Feed = FeedViewModel.FromFeed(feedDB.GetFeedById(Feed.Id));

            feedTitle.Text = Feed.Title;
            feedInput.Text = Feed.URL;

            String hiResFavicon = await feedDB.GetFeedById(Feed.Id).getHiResFaviconAsync();
            feedHQFavicon.Source = new BitmapImage(new Uri(hiResFavicon, UriKind.Absolute));

            atomIconToggle.IsOn = Feed.usingAtomIcon;
            ResourceLoader rl = new ResourceLoader();
            if (Feed.TileXML != rl.GetString("AdaptiveTemplate"))
            {
                customTileToggle.IsOn = true;
                customTileXMLContent.Text = Feed.TileXML;
                customTileXML.Visibility = Visibility.Visible;
            }
            else
            {
                customTileToggle.IsOn = false;
                customTileXML.Visibility = Visibility.Collapsed;
            }

            pinButton.Visibility = Visibility.Collapsed;
            unpinButton.Visibility = Visibility.Collapsed;
            outputStackPanel.Visibility = Visibility.Collapsed;

            //Check if feed is pinned and set visibility of buttons in consequence
            if (feedDB.GetFeedById(Feed.Id).IsTilePinned())
            {
                readButton.Visibility = Visibility.Visible;
                unpinButton.Visibility = Visibility.Visible;
            }
            else if (feedDB.GetFeedById(Feed.Id).IsTileValid())
            {
                pinButton.Visibility = Visibility.Visible;
                readButton.Visibility = Visibility.Visible;
            }



            FeedWaiting.Visibility = Visibility.Collapsed;
            //We rebuilt the UI, now fade it in
            RootPanel.Opacity = 0.0;
            RootPanel.Visibility = Visibility.Visible;

            AnimateDouble(RootPanel, "Opacity", 1.0, 200, () =>
            {
                RootPanel.Visibility = Visibility.Visible;
            });
        }

        public static void AnimateDouble(DependencyObject target, string path, double to, double duration, Action onCompleted = null)
        {
            var animation = new DoubleAnimation
            {
                EnableDependentAnimation = true,
                To = to,
                Duration = new Duration(TimeSpan.FromMilliseconds(duration))
            };
            Storyboard.SetTarget(animation, target);
            Storyboard.SetTargetProperty(animation, path);

            var sb = new Storyboard();
            sb.Children.Add(animation);

            if (onCompleted != null)
            {
                sb.Completed += (s, e) =>
                {
                    onCompleted();
                };
            }

            sb.Begin();
        }
        
        private async void DeleteFeed(object sender, RoutedEventArgs e)
        {

            var dialog = new MessageDialog("Do you really want to delete " + Feed.Title + "? \nThe matching Live Tile will be unpinned.", "Delete Feed?");

            dialog.Commands.Add(new Windows.UI.Popups.UICommand("Yes") { Id = 0 });
            dialog.Commands.Add(new Windows.UI.Popups.UICommand("No") { Id = 1 });

            dialog.DefaultCommandIndex = 0;
            dialog.CancelCommandIndex = 1;

            var result = await dialog.ShowAsync();

            if ((int)result.Id == 0)
            {
                FeedDataSource feedSrc = new FeedDataSource();
                feedSrc.GetFeedById(Feed.Id).UnpinTileAsync();
                feedSrc.DeleteFeed(Feed.Id);
            }
        }

        private async void AtomIcon_Toggled(object sender, RoutedEventArgs e)
        {
            //Clear the existing image first to show we mean business
            feedHQFavicon.Source = new BitmapImage(new Uri("ms-appx:///Assets/loadan.gif"));

            //Show the new icon on the page, but don't save anything.
            Feed f = await BuildFeedFromPage();

            ToggleSwitch toggleSwitch = sender as ToggleSwitch;
            if (toggleSwitch != null)
            {
                f.SetAtomIconUse(toggleSwitch.IsOn);
                String newIconURL = await f.getHiResFaviconAsync();
                feedHQFavicon.Source = new BitmapImage(new Uri(newIconURL, UriKind.Absolute));
            }

        }

        private async void ReadFeed(object sender, RoutedEventArgs e)
        {
            Feed f = await BuildFeedFromPage();

            CoreApplicationView newView = CoreApplication.CreateNewView();
            int newViewId = 0;
            await newView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Frame frame = new Frame();
                frame.Navigate(typeof(FeedReader), f.GetId().ToString());
                Window.Current.Content = frame;
                // You have to activate the window in order to show it later.
                Window.Current.Activate();

                newViewId = ApplicationView.GetForCurrentView().Id;
            });
            bool viewShown = await ApplicationViewSwitcher.TryShowAsStandaloneAsync(newViewId);
        }
    }
}

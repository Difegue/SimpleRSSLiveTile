using BackgroundTasks;
using RSSDataTypes.Data;
using SimpleRSSLiveTile.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Store;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace SimpleRSSLiveTile
{
    public sealed partial class NewMainPage : Page
    {
        private FeedViewModel _lastSelectedFeed;
        private ObservableCollection<FeedViewModel> FeedList;
        private LicenseInformation doshMoneyDollar;

        public NewMainPage()
        {
            this.InitializeComponent();

        }

        //Trial/Non Trial differentiation. (Pretty trivial but fun stuff)
        //Still not used 
        private async void initializeIAP()
        {
            // Get the license info
            // The next line is commented out for testing.
            // doshMoneyDollar = CurrentApp.LicenseInformation;

            // The next line is commented out for production/release.  
            StorageFile proxyFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/WindowsStoreProxy.xml"));
            await CurrentAppSimulator.ReloadSimulatorAsync(proxyFile);

            doshMoneyDollar = CurrentAppSimulator.LicenseInformation;

            if (doshMoneyDollar.ProductLicenses["donationFromTomodachi"].IsActive)
            {
                donateButton.Label = "Arigato !";
                
                FontIcon fIcon = new FontIcon();
                fIcon.Glyph = "🤑";
                donateButton.Icon = fIcon;

            }

        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            //Register background task
            BackgroundTaskHandler.RegisterBackgroundTask();

            FeedList = new ObservableCollection<FeedViewModel>();

            FeedDataSource feedSrc = new FeedDataSource();
            
            foreach (var Feed in feedSrc.GetAllFeeds())
            {
                FeedViewModel viewModel = FeedViewModel.FromFeed(Feed);
                FeedList.Add(viewModel);
            }

            if (e.Parameter != null && e.Parameter.ToString() != "")
            {
                // Parameter is Feed ID
                int id = (int)e.Parameter;
                _lastSelectedFeed =
                    FeedList.Where((Feed) => Feed.Id == id).FirstOrDefault();
            }

            MasterListView.ItemsSource = FeedList;
            UpdateForVisualState(AdaptiveStates.CurrentState);

            //initializeIAP();
        }

        private void UpdateFeedList()
        {
            FeedList.Clear();
            FeedDataSource feedSrc = new FeedDataSource();
            foreach (var Feed in feedSrc.GetAllFeeds())
            {
                FeedViewModel viewModel = FeedViewModel.FromFeed(Feed);
                FeedList.Add(viewModel);
            }
        }

        private void MasterListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var clickedFeed = (FeedViewModel)e.ClickedItem;
            _lastSelectedFeed = clickedFeed;

            if (AdaptiveStates.CurrentState == NarrowState)
            {
                // Use "drill in" transition for navigating from master list to detail view
                Frame.Navigate(typeof(FeedDetailPage), clickedFeed.Id, new DrillInNavigationTransitionInfo());
            }
            else
            {
                deleteFeedButton.Visibility = Visibility.Visible;
                // Play a refresh animation when the user switches detail Feeds.
                //EnableContentTransitions();
                contentFrame.Children.Clear();
                contentFrame.VerticalAlignment = VerticalAlignment.Stretch;
                Frame f = new Frame();
                f.Navigate(typeof(FeedDetailPage), clickedFeed.Id);
                contentFrame.Children.Add(f);
               
            }
        }

        private async void DeleteSelectedFeed(object sender, RoutedEventArgs e)
        {
            //Get Feed we want to delete
            if (_lastSelectedFeed != null)
            {
                var dialog = new MessageDialog("Do you really want to delete "+_lastSelectedFeed.Title+"? \nThe matching Live Tile will be unpinned.", "Delete Feed?");

                dialog.Commands.Add(new Windows.UI.Popups.UICommand("Yes") { Id = 0 });
                dialog.Commands.Add(new Windows.UI.Popups.UICommand("No") { Id = 1 });

                dialog.DefaultCommandIndex = 0;
                dialog.CancelCommandIndex = 1;

                var result = await dialog.ShowAsync();

                if ((int)result.Id == 0)
                {
                    deleteFeedButton.Visibility = Visibility.Collapsed;
                    FeedDataSource feedSrc = new FeedDataSource();
                    feedSrc.GetFeedById(_lastSelectedFeed.Id).UnpinTileAsync();
                    feedSrc.DeleteFeed(_lastSelectedFeed.Id);          
                    UpdateFeedList();
                    _lastSelectedFeed = null;
                    contentFrame.Children.Clear();
                }
            }
        }

        private async void OpenDonate(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=sugoi%40cock%2eli&lc=FR&item_name=Sugoi%20Apps%20for%20Tomodachis%20United&currency_code=EUR&bn=PP%2dDonationsBF%3abtn_donate_LG%2egif%3aNonHosted"));
            //string aboutDialog = "If you find this thing useful, consider buying the app ! Or at ";
            //MessageDialog msgbox = new MessageDialog(aboutDialog, "Here comes the money");
            //await msgbox.ShowAsync();
        }

        private async void AboutSplash(object sender, RoutedEventArgs e)
        {
            string aboutDialog = "Previously Simple RSS Live Tile. \nI still use this for my bank. \nSource code available 👉 https://github.com/Difegue/SimpleRSSLiveTile";
            MessageDialog msgbox = new MessageDialog(aboutDialog, "About RSS Live Tiles 👌🐔");
            await msgbox.ShowAsync();
        }

        //Add a new blank feed, and refresh the page so it appears in the list.
        private void AddFeed(object sender, RoutedEventArgs e)
        {
            
            FeedDataSource feedDB = new FeedDataSource();

            //Unique ID. Unless people add tons of feeds(at which point they're probably better off using something else), 
            //collisions have no reason to happen.
            Random random = new Random();
            int i = random.Next();

            Feed f = new Feed(i, "New RSS Feed", "http://example.com/");

            feedDB.SetFeed(f);

            FeedViewModel viewModel = FeedViewModel.FromFeed(f);
            FeedList.Add(viewModel);

        }

        private void AdaptiveStates_CurrentStateChanged(object sender, VisualStateChangedEventArgs e)
        {
            UpdateForVisualState(e.NewState, e.OldState);
        }

        private void UpdateForVisualState(VisualState newState, VisualState oldState = null)
        {
            var isNarrow = newState == NarrowState;

            if (isNarrow && oldState == DefaultState && _lastSelectedFeed != null)
            {
                // Resize down to the detail Feed. Don't play a transition.
                contentFrame.Children.Clear();
                Frame.Navigate(typeof(FeedDetailPage), _lastSelectedFeed.Id, new SuppressNavigationTransitionInfo());
            }

            EntranceNavigationTransitionInfo.SetIsTargetElement(MasterListView, isNarrow);

        }


        private void LayoutRoot_Loaded(object sender, RoutedEventArgs e)
        {
            // Assure we are displaying the correct Feed. This is necessary in certain adaptive cases.
            MasterListView.SelectedItem = _lastSelectedFeed;
        }

        private async void SendFeedback(object sender, RoutedEventArgs e)
        {
            var emailMessage = new Windows.ApplicationModel.Email.EmailMessage();
            emailMessage.Subject = "Feedback for RSS Live Tiles";

            string feedList = "RSS Feeds used : \n";
            FeedDataSource feedSrc = new FeedDataSource();
            foreach (var Feed in feedSrc.GetAllFeeds())
            {
                feedList = feedList + Feed.GetURL() + "\n";
            }

            emailMessage.Body = feedList;

            var emailRecipient = new Windows.ApplicationModel.Email.EmailRecipient("sugoi@cock.li");
            emailMessage.To.Add(emailRecipient);
            

            await Windows.ApplicationModel.Email.EmailManager.ShowComposeNewEmailAsync(emailMessage);
        }
    }
}

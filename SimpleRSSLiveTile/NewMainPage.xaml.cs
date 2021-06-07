using BackgroundTasks;
using Microsoft.Services.Store.Engagement;
using RSSDataTypes.Data;
using SimpleRSSLiveTile.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Services.Store;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace SimpleRSSLiveTile
{

    public sealed partial class NewMainPage : Windows.UI.Xaml.Controls.Page
    {

        private FeedViewModel _lastSelectedFeed;
        private TrulyObservableCollection<FeedViewModel> FeedList;

        private StoreContext context = null;
        private string donationStoreID = "9nblggh50zp6";

        public NewMainPage()
        {
            this.InitializeComponent();

        }

        //Check for add-on purchases.
        private async void initializeIAP()
        {
            // Get the license info
            context = StoreContext.GetDefault();

            StoreAppLicense appLicense = await context.GetAppLicenseAsync();

            // Access the add on licenses for add-ons for this app.
            foreach (KeyValuePair<string, StoreLicense> item in appLicense.AddOnLicenses)
            {
                StoreLicense addOnLicense = item.Value;

                if (addOnLicense.IsActive) //We only have one add-on in this use-case, so this is fine. 
                    AcknowledgeDonation();
            }

        }

        private void AcknowledgeDonation()
        {
            donateButton.Content = "Arigato !";
            donateButton.Icon.Foreground = new SolidColorBrush(Colors.DarkGoldenrod);
            donateButton.IsEnabled = false;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            //Register background task
            BackgroundTaskHandler.RegisterBackgroundTask();

            //Push Notification configuration
            StoreServicesEngagementManager engagementManager = StoreServicesEngagementManager.GetDefault();
            engagementManager.RegisterNotificationChannelAsync();

            FeedList = new TrulyObservableCollection<FeedViewModel>();
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

            //Look if user has add-on purchases registered.
            initializeIAP();

            //Update reference to mainPage in GlobalReference
            GlobalMainPageReference.mainPage = this;

            ApplicationViewTitleBar systemTitleBar = ApplicationView.GetForCurrentView().TitleBar;
            systemTitleBar.ButtonInactiveBackgroundColor = (Color)Application.Current.Resources["SystemAltMediumColor"];
        }

        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            // NavView doesn't load any page by default: you need to specify it
            ContentFrame.Navigate(typeof(LandingPage));
        }

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            // Getting the Feed from Content (args.InvokedItem is the content of NavigationViewItem)
            var clickedFeed = (FeedViewModel)args.InvokedItem;
            _lastSelectedFeed = clickedFeed;

            //Navigate to another page quickly so we can re-navigate to FeedDetail if we're already on it
            ContentFrame.Navigate(typeof(FeedDetailPage), clickedFeed);
        }

        private void NavView_BackRequested(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            On_BackRequested();
        }

        private void BackInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            On_BackRequested();
            args.Handled = true;
        }

        private bool On_BackRequested()
        {
            if (!ContentFrame.CanGoBack)
                return false;

            // Don't go back if the nav pane is overlayed
            if (NavView.IsPaneOpen &&
                (NavView.DisplayMode == NavigationViewDisplayMode.Compact ||
                NavView.DisplayMode == NavigationViewDisplayMode.Minimal))
                return false;

            ContentFrame.GoBack();
            return true;
        }

        //Update the viewmodel list depending on the the contents of the FeedDataSource.
        public void UpdateFeedList()
        {
            //Can't clear the feedList as that'd break the NavigationView
            FeedDataSource feedSrc = new FeedDataSource();

            //Clear deleted feeds' viewmodels
            foreach (FeedViewModel feedVM in FeedList)
            {
                if (!feedSrc.FeedExists(feedVM.Id.ToString()))
                    FeedList.Remove(feedVM);
                else
                    feedVM.Update();
            }

            //Add viewmodels for newly created feeds
            foreach (var Feed in feedSrc.GetAllFeeds().Where(x => FeedList.Count(y => y.Id == x.GetId()) == 0))
            { 
                FeedViewModel viewModel = FeedViewModel.FromFeed(Feed);

                if (!FeedList.Contains(viewModel))
                    FeedList.Add(viewModel);
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
                    //deleteFeedButton.Visibility = Visibility.Collapsed;
                    FeedDataSource feedSrc = new FeedDataSource();
                    feedSrc.GetFeedById(_lastSelectedFeed.Id).UnpinTileAsync();
                    await feedSrc.DeleteFeed(_lastSelectedFeed.Id);
                    FeedList.Remove((FeedViewModel)NavView.SelectedItem);

                    _lastSelectedFeed = null;
                    ContentFrame.Navigate(typeof(LandingPage));
                }
            } else
            {
                await new MessageDialog("Please select a Feed to delete first.", "No Feed to delete!").ShowAsync();
            }
        }

        //Add a new blank feed, and refresh the page so it appears in the list.
        private void AddFeed(object sender, RoutedEventArgs e)
        {
            
            FeedDataSource feedDB = new FeedDataSource();

            //Unique ID. 
            Random random = new Random();
            int i = random.Next();

            //It's like I'm really programming pomf.se
            while (feedDB.FeedExists(i.ToString()))
                { i = random.Next(); }

            Feed f = new Feed(i, "New RSS Feed", "http://example.com/");

            feedDB.SetFeed(f);

            FeedViewModel viewModel = FeedViewModel.FromFeed(f);
            FeedList.Add(viewModel);

        }

        private async void SendFeedback(object sender, RoutedEventArgs e)
        {
            var launcher = StoreServicesFeedbackLauncher.GetDefault();
            await launcher.LaunchAsync();
        }

        private void OpenExamples(object sender, RoutedEventArgs e)
        {
            ContentFrame.Navigate(typeof(ExamplePage));
        }

        private async void OpenDonate(object sender, RoutedEventArgs e)
        {
            StorePurchaseResult result = await context.RequestPurchaseAsync(donationStoreID);

            switch (result.Status)
            {
                case StorePurchaseStatus.AlreadyPurchased:
                    AcknowledgeDonation();
                    break;

                case StorePurchaseStatus.Succeeded:
                    AcknowledgeDonation();
                    break;

                default:
                    break;
            }
        }

        private void OpenAbout(object sender, RoutedEventArgs e)
        {
            ContentFrame.Navigate(typeof(AboutPage));
        }

    }
}

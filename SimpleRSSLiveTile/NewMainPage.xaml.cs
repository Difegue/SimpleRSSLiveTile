using BackgroundTasks;
using Microsoft.Services.Store.Engagement;
using RSSDataTypes.Data;
using SimpleRSSLiveTile.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Services.Store;
using Windows.Storage;
using Windows.System;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
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

    //ViewModel for the NavView Header, which also contains the custom Titlebar.
    public class CustomTitleBar
    {
        public SolidColorBrush TitleBarBackground { get; set; }
        public SolidColorBrush TitleBarForeground { get; set; }

        public CustomTitleBar()
        {
            TitleBarBackground = new SolidColorBrush((Color)Application.Current.Resources["SystemAccentColor"]);
            TitleBarForeground = new SolidColorBrush((Color)Application.Current.Resources["SystemAltHighColor"]);
        }
    }

    public sealed partial class NewMainPage : Page
    {

        private FeedViewModel _lastSelectedFeed;
        private TrulyObservableCollection<FeedViewModel> FeedList;

        private StoreContext context = null;
        private string donationStoreID = "9nblggh50zp6";

        private CustomTitleBar titleBar = new CustomTitleBar();

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

            Window.Current.Activated += UpdateTitleBar;
            //Set our custom titleBar as the header so colors propagate properly.
            NavView.Header = titleBar;

            ApplicationViewTitleBar systemTitleBar = ApplicationView.GetForCurrentView().TitleBar;
            systemTitleBar.ButtonInactiveBackgroundColor = (Color)Application.Current.Resources["SystemAltMediumColor"];
        }

        private void UpdateTitleBar(object sender, Windows.UI.Core.WindowActivatedEventArgs e)
        {

            if (e.WindowActivationState == Windows.UI.Core.CoreWindowActivationState.Deactivated)
            {
                titleBar.TitleBarBackground.Color = (Color)Application.Current.Resources["SystemAltMediumColor"];
                titleBar.TitleBarForeground.Color = Color.FromArgb(255, 153, 153, 153);
            } else
            {
                titleBar.TitleBarBackground.Color = (Color)Application.Current.Resources["SystemAccentColor"];
                titleBar.TitleBarForeground.Color = (Color)Application.Current.Resources["SystemAltHighColor"];
            }
           
        }

        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            // NavView doesn't load any page by default: you need to specify it
            ContentFrame.Navigate(typeof(WelcomePage));
        }

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            // Getting the Feed from Content (args.InvokedItem is the content of NavigationViewItem)
            var clickedFeed = (FeedViewModel)args.InvokedItem;
            _lastSelectedFeed = clickedFeed;

            //Navigate to another page quickly so we can re-navigate to FeedDetail if we're already on it
            //ContentFrame.Navigate(typeof(WelcomePage));
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
                    feedSrc.DeleteFeed(_lastSelectedFeed.Id);
                    FeedList.Remove((FeedViewModel)NavView.SelectedItem);

                    _lastSelectedFeed = null;
                    ContentFrame.Navigate(typeof(WelcomePage));
                }
            } else
            {
                await new MessageDialog("Please select a Feed to delete first.", "No Feed to delete!").ShowAsync();
            }
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

        //Add a new blank feed, and refresh the page so it appears in the list.
        private void AddFeed(object sender, RoutedEventArgs e)
        {
            
            FeedDataSource feedDB = new FeedDataSource();

            //Unique ID. 
            int i = 1337;
            Random random = new Random();

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

        private void OpenExamples(object sender, RoutedEventArgs e)
        {
            ContentFrame.Navigate(typeof(ExamplePage));
        }

        private void OpenAbout(object sender, RoutedEventArgs e)
        { 
            ContentFrame.Navigate(typeof(AboutPage));
        }

    }
}

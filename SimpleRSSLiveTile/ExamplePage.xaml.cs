using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using RSSDataTypes.Data;
using Windows.UI.Popups;
using Windows.Foundation.Metadata;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Core;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace SimpleRSSLiveTile
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ExamplePage : Page
    {
        private FeedDataSource feedStorage;
        private ResourceLoader rl = new ResourceLoader();

        public ExamplePage()
        {
            this.InitializeComponent();
            feedStorage = new FeedDataSource();
        }

        private async void AddExample3(object sender, RoutedEventArgs e)
        {
            string exampleXML = rl.GetString("Example3XML");
            bool res = await AddExampleGeneric("38429133", "Example Feed 3", "https://www.reddit.com/r/all/.rss", exampleXML);

            if (res)
            {
                Button3.IsEnabled = false;
                Button3.Content = "All Set!";
                GlobalMainPageReference.mainPage.UpdateFeedList();
            }
        }

        private async void AddExample2(object sender, RoutedEventArgs e)
        {
            string exampleXML = rl.GetString("Example2XML");
            bool res = await AddExampleGeneric("38429132", "Example Feed 2", "https://xkcd.com/rss.xml", exampleXML);

            if (res)
            {
                Button2.IsEnabled = false;
                Button2.Content = "All Set!";
                GlobalMainPageReference.mainPage.UpdateFeedList();
            }
        }

        private async void AddExample1(object sender, RoutedEventArgs e)
        {
           string exampleXML = rl.GetString("Example1XML");
           bool res = await AddExampleGeneric("38429131", "Example Feed 1", "http://workplace.stackexchange.com/feeds", exampleXML);
           
           if (res)
           {
                Button1.IsEnabled = false;
                Button1.Content = "All Set!";
                GlobalMainPageReference.mainPage.UpdateFeedList();
           }
        }


        //Adds the specified example feed
        private async Task<bool> AddExampleGeneric(string idFeed, string titleFeed, string urlFeed, string XMLFeed)
        {
            Feed example = new Feed(Int32.Parse(idFeed), titleFeed, urlFeed, XMLFeed);

            if (!feedStorage.FeedExists(idFeed))
            {
                feedStorage.SetFeed(example);
                return true;
            }
            else
            {
                string title = await feedStorage.GetFeedById(Int32.Parse(idFeed)).GetFeedTitleAsync();
                bool result = await ShowReplaceDialogAsync(title);
                if (result)
                {
                    await feedStorage.DeleteFeed(Int32.Parse(idFeed));
                    feedStorage.SetFeed(example);
                    return true;
                }
                return false;
            }
        }

        private async Task<bool> ShowReplaceDialogAsync(string feedName)
        {
            string title = "Feed already exists";
            string content = "You already have a feed using the same ID as the Example's. \r\n(Its name is " + feedName+".)\r\nDo you want to delete this Feed and add the Example instead?";

            UICommand yesCommand = new UICommand("Yes");
            UICommand noCommand = new UICommand("No");
            UICommand cancelCommand = new UICommand("Cancel");

            var dialog = new MessageDialog(content, title);
            dialog.Options = MessageDialogOptions.None;
            dialog.Commands.Add(yesCommand);

            dialog.DefaultCommandIndex = 0;
            dialog.CancelCommandIndex = 0;

            if (noCommand != null)
            {
                dialog.Commands.Add(noCommand);
                dialog.CancelCommandIndex = (uint)dialog.Commands.Count - 1;
            }

            if (cancelCommand != null)
            {
                // Devices with a hardware back button
                // use the hardware button for Cancel.
                // for other devices, show a third option

                var t_hardwareBackButton = "Windows.Phone.UI.Input.HardwareButtons";

                if (ApiInformation.IsTypePresent(t_hardwareBackButton))
                {
                    // disable the default Cancel command index
                    // so that dialog.ShowAsync() returns null
                    // in that case

                    dialog.CancelCommandIndex = UInt32.MaxValue;
                }
                else
                {
                    dialog.Commands.Add(cancelCommand);
                    dialog.CancelCommandIndex = (uint)dialog.Commands.Count - 1;
                }
            }

            var command = await dialog.ShowAsync();

            if (command == null && cancelCommand != null)
            {
                // back button was pressed
                // invoke the UICommand

                cancelCommand.Invoked(cancelCommand);
            }

            if (command == yesCommand)
            {
                return true;
            }
            else if (command == noCommand)
            {
                return false;
            }
            else
            {
                return false;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var backStack = Frame.BackStack;
            var backStackCount = backStack.Count;

            // Register for hardware and software back request from the system
            SystemNavigationManager systemNavigationManager = SystemNavigationManager.GetForCurrentView();
            systemNavigationManager.BackRequested += DetailPage_BackRequested;
            if (!ShouldGoToWideState())
                systemNavigationManager.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;

        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            SystemNavigationManager systemNavigationManager = SystemNavigationManager.GetForCurrentView();
            systemNavigationManager.BackRequested -= DetailPage_BackRequested;
            systemNavigationManager.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
        }

        private void DetailPage_BackRequested(object sender, BackRequestedEventArgs e)
        {
            // Mark event as handled so we don't get bounced out of the app.
            e.Handled = true;

            OnBackRequested();
        }

        private void OnBackRequested()
        {
            // Page above us will be our master view.
            // Make sure we are using the "drill out" animation in this transition.
            if (Frame != null && Frame.BackStackDepth > 0)
                Frame.GoBack(new DrillInNavigationTransitionInfo());
        }

        void NavigateBackForWideState(bool useTransition)
        {
            // Evict this page from the cache as we may not need it again.
            NavigationCacheMode = NavigationCacheMode.Disabled;

            if (Frame != null && Frame.BackStackDepth > 0)
                if (useTransition)
                {
                    Frame.GoBack(new EntranceNavigationTransitionInfo());
                }
                else
                {
                    Frame.GoBack(new SuppressNavigationTransitionInfo());
                }

        }

        private bool ShouldGoToWideState()
        {
            return Window.Current.Bounds.Width >= 720;
        }
    }
}

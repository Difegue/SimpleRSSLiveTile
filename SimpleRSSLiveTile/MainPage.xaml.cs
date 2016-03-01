using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.ApplicationModel.Background;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Popups;
using Windows.ApplicationModel.Resources;
using Windows.Data.Xml.Dom;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace SimpleRSSLiveTile
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

        public MainPage()
        {
            this.InitializeComponent();

            Object savedFeed = localSettings.Values["RSSfeed"];
            Object customXML = localSettings.Values["customXML"];

            if (customXML !=null && (string)customXML!="")
            {
                customTileToggle.IsOn = true;
                customTileXML.Visibility = Visibility.Visible;
                customTileXMLContent.Text = (String)customXML;
            }
            else
            {
                ResourceLoader rl = new ResourceLoader();
                customTileXMLContent.Text = rl.GetString("AdaptiveTemplate");
                
            }

            if (savedFeed != null)
                feedInput.Text = (String)savedFeed;


        }

        private async void RegisterBackgroundTask()
        {
            var backgroundAccessStatus = await BackgroundExecutionManager.RequestAccessAsync();
            if (backgroundAccessStatus == BackgroundAccessStatus.AllowedMayUseActiveRealTimeConnectivity ||
                backgroundAccessStatus == BackgroundAccessStatus.AllowedWithAlwaysOnRealTimeConnectivity)
            {
                foreach (var task in BackgroundTaskRegistration.AllTasks)
                {
                    if (task.Value.Name == taskName)
                    {
                        task.Value.Unregister(true);
                    }
                }

                BackgroundTaskBuilder taskBuilder = new BackgroundTaskBuilder();
                taskBuilder.Name = taskName;
                taskBuilder.TaskEntryPoint = taskEntryPoint;
                taskBuilder.SetTrigger(new TimeTrigger(15, false));
                var registration = taskBuilder.Register();
            }
        }

        private const string taskName = "RSSFeedBackgroundTask";
        private const string taskEntryPoint = "BackgroundTasks.RSSFeedBackgroundTask";

        private async void Save_Feed(object sender, RoutedEventArgs e)
        {
            Progress.Visibility = Windows.UI.Xaml.Visibility.Visible;

            localSettings.Values["RSSfeed"] = feedInput.Text;
            this.RegisterBackgroundTask();

            greetingOutput.Foreground = new SolidColorBrush(Windows.UI.Colors.Green);
            string result = "Feed saved! Pin the Application now.";

            try
            {
                await BackgroundTasks.RSSFeedLiveTile.Update();
            }
            catch (Exception exp)
            {
                //Red error text wow
                greetingOutput.Foreground = new SolidColorBrush(Windows.UI.Colors.Red);
                result = "Error while parsing the Feed!\n(" + exp.Message +")";

                //wipe the feed URL in case the user quits in order to not have fucked background tasks
                localSettings.Values["RSSfeed"] = null;

            }

            Progress.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            greetingOutput.Text = result;
        }

        private async void Open_Donate(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=sugoi%40cock%2eli&lc=FR&item_name=Sugoi%20Apps%20for%20Tomodachis%20United&currency_code=EUR&bn=PP%2dDonationsBF%3abtn_donate_LG%2egif%3aNonHosted"));
        }

        private async void About_Splash(object sender, RoutedEventArgs e)
        {
            string aboutDialog = "I pretty much only made this because my bank is awful \nPlease give me money I have anime figures to buy";
            MessageDialog msgbox = new MessageDialog(aboutDialog, "About Simple RSS Live Tile");
            await msgbox.ShowAsync();
        }

        private void ToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            ToggleSwitch toggleSwitch = sender as ToggleSwitch;
            if (toggleSwitch != null)
            {
                if (toggleSwitch.IsOn == true)
                    customTileXML.Visibility = Visibility.Visible;
                else
                {
                    customTileXML.Visibility = Visibility.Collapsed;
                    localSettings.Values["customXML"] = null;
                    BackgroundTasks.RSSFeedLiveTile.Update();
                }
            }
        }

        private void Show_FormattingHelp(object sender, TappedRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
        }

        private void saveCustomXML_Click(object sender, RoutedEventArgs e)
        {
            Progress.Visibility = Windows.UI.Xaml.Visibility.Visible;
            XmlDocument tileXml = new XmlDocument();
            

            bool failed = false;

            try
            {
                tileXml.LoadXml(customTileXMLContent.Text);
            }
            catch(Exception exp) //if we can't load the XML, it's probably not valid.
            {
                customXMLOutput.Foreground = new SolidColorBrush(Windows.UI.Colors.Red);
                customXMLOutput.Text = "Invalid XML. \n("+exp.Message+")";
                failed = true;
            }
           
            if (!failed)
            {
                if (localSettings.Values["RSSfeed"]!=null)
                    BackgroundTasks.RSSFeedLiveTile.Update();

                customXMLOutput.Text = "XML saved ! Live Tile has been updated.";
                localSettings.Values["customXML"] = customTileXMLContent.Text;
                customXMLOutput.Foreground = new SolidColorBrush(Windows.UI.Colors.Green);
            }

            Progress.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

        }
    }
}

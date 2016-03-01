using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.ApplicationModel.Resources.Core;

// Added during quickstart
using Windows.ApplicationModel.Background;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using Windows.Web.Syndication;

namespace BackgroundTasks
{

    public sealed class RSSFeedLiveTile
    {
        // Although most HTTP servers do not require User-Agent header, others will reject the request or return 
        // a different response if this header is missing. Use SetRequestHeader() to add custom headers. 
        static string customHeaderName = "User-Agent";
        static string customHeaderValue = "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)";

        //Name of the XML tags that are filled with the RSS data in the Tile Template.
        static string[] textElementName = { "title1", "title2", "title3", "title4" };
        static string[] descElementName = { "desc1", "desc2", "desc3", "desc4" };

        static Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

        private static async Task<SyndicationFeed> GetRSSFeed()
        {
            SyndicationFeed feed = null;

            try
            {
                // Create a syndication client that downloads the feed.  
                SyndicationClient client = new SyndicationClient();
                client.BypassCacheOnRetrieve = true;
                client.SetRequestHeader(customHeaderName, customHeaderValue);

                //Get feed URL from App 
                String feedUrl = (String)localSettings.Values["RSSFeed"];

                // Download the feed. 
                Uri feedUri = new Uri(feedUrl);
                feed = await client.RetrieveFeedAsync(feedUri);

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            return feed;
        }

        private static void UpdateTile(SyndicationFeed feed)
        {

            if (feed == null)
            {
                System.ArgumentException argEx = new System.ArgumentException("Not a valid Feed");
                throw argEx;
            }

            // Create a tile update manager for the specified syndication feed.
            var updater = TileUpdateManager.CreateTileUpdaterForApplication();
            updater.EnableNotificationQueue(true);
            updater.Clear();

            // Keep track of the number feed items that get tile notifications. 
            int itemCount = 0;

            //Open up Tile Template: If a custom schema exists, use that. Otherwise, use the default.
            string tileTmplt;

            if (localSettings.Values["customXML"] != null)
                tileTmplt = (string)localSettings.Values["customXML"];
            else
            {
                ResourceLoader rl = new ResourceLoader();
                tileTmplt = rl.GetString("AdaptiveTemplate");
            }

            //Create tiles
            XmlDocument tileXml = new Windows.Data.Xml.Dom.XmlDocument();
            tileXml.LoadXml(tileTmplt);

            //Edit tile title
            XmlElement feedTitle = (XmlElement)tileXml.GetElementsByTagName("visual")[0];

            String feedTitleText = feed.Title.Text == null ? feed.BaseUri.ToString() : feed.Title.Text;
            feedTitle.SetAttribute("displayName", feedTitleText);

            // Grab feed items and add them to the tiles.
            foreach (var item in feed.Items)
            {
                //item.Summary;
                var title = item.Title;
                var desc = item.Summary;

                string titleText = title == null ? String.Empty : title.Text;
                if (titleText.Length > 200)
                   titleText = titleText.Substring(0, 200);

                string titleDesc = desc == null ? String.Empty :desc.Text;
                if (titleDesc.Length > 200)
                   titleDesc = titleDesc.Substring(0, 200);

                XmlNodeList nodeListTitle = tileXml.GetElementsByTagName(textElementName[itemCount]);
                XmlNodeList nodeListDesc = tileXml.GetElementsByTagName(descElementName[itemCount]);

                foreach (IXmlNode node in nodeListTitle)
                    node.InnerText = titleText;

                foreach (IXmlNode node in nodeListDesc)
                    node.InnerText = titleDesc;

                // Don't get more than 3 items.
                itemCount++;
                if (itemCount > 2)
                    break;
            }

            String cmplteTile = tileXml.GetXml();

            //The tags used to insert the RSS elements in the XML need to be removed for the tile to properly appear in the Start Menu.
            //HERE COMES THE QUALITY
            foreach (string tag in textElementName)
            {
                cmplteTile = cmplteTile.Replace("<" + tag + ">", "");
                cmplteTile = cmplteTile.Replace("</" + tag + ">", "");
            }

            foreach (string tag in descElementName)
            {
                cmplteTile = cmplteTile.Replace("<" + tag + ">", "");
                cmplteTile = cmplteTile.Replace("</" + tag + ">", "");
            }

            //Reload the XML after corrections have been made
            tileXml.LoadXml(cmplteTile);

            //Send the notifications.
            updater.Update(new TileNotification(tileXml));

        }

        //Method for updating the tile within the App.


        public static Windows.Foundation.IAsyncAction Update()
        {
            return Task.Run(async () =>
            {
                // Download the feed.
                var feed = await GetRSSFeed();

                // Update the live tile with the feed items.
                UpdateTile(feed);

            }).AsAsyncAction();
        }


    }

    //Background Task Version.
    public sealed class RSSFeedBackgroundTask : IBackgroundTask
    {
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            // Get a deferral, to prevent the task from closing prematurely 
            // while asynchronous code is still running.
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();

            await RSSFeedLiveTile.Update();

            // Inform the system that the task is finished.
            deferral.Complete();
        }
        
    }

}


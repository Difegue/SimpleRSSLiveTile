using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using Windows.UI.StartScreen;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Web.Syndication;

namespace SimpleRSSLiveTile.Data
{
    public class Feed
    {
        // Although most HTTP servers do not require User-Agent header, others will reject the request or return 
        // a different response if this header is missing. Use SetRequestHeader() to add custom headers. 
        static string customHeaderName = "User-Agent";
        static string customHeaderValue = "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)";

        //Name of the XML tags that are filled with the RSS data in the Tile Template.
        static string[] textElementName = { "title1", "title2", "title3", "title4" };
        static string[] descElementName = { "desc1", "desc2", "desc3", "desc4" };

        [JsonProperty] private int feedId;
        [JsonProperty] private string feedTitle;
        [JsonProperty] private string URL;
        [JsonProperty] private string customXml;
        [JsonProperty] private bool isPinned;
        [JsonProperty] private bool isValid;


        public Feed()
        {
        }

        public Feed(int i, string t, string u)
        {
            ResourceLoader rl = new ResourceLoader();

            feedId = i;
            feedTitle = t;
            URL = u;
            customXml = rl.GetString("AdaptiveTemplate");
            isPinned = false;
            isValid = false;
        }

        public Feed(int i, string t, string u, string x)
        {
            feedId = i;
            feedTitle = t;
            URL = u;
            customXml = x;
            isPinned = false;
            isValid = false;
        }

        //Pins Tile to Start Menu.
        public void pinTile()
        {

            isPinned = true;
        }

        //Unpins Tile from Start Menu.
        public void unpinTile()
        {

            isPinned = false;
        }

        //Get the RSS Feed's title, or the URL if it doesn't have one.
        public async Task<String> getFeedTitle()
        {

            SyndicationFeed feed = await getFeedData();
            if (feed != null)
            {
                String feedTitleText = feed.Title.Text == null ? feed.BaseUri.ToString() : feed.Title.Text;
                isValid = true;
                return feedTitleText;
            }

            isValid = false;
            return "Invalid RSS Feed";
        }


        //Get RSS feed from URL, if it's incorrect return null
        private async Task<SyndicationFeed> getFeedData()
        {
            SyndicationFeed feed = null;

            try
            {
                // Create a syndication client that downloads the feed.  
                SyndicationClient client = new SyndicationClient();
                client.BypassCacheOnRetrieve = true;
                client.SetRequestHeader(customHeaderName, customHeaderValue);

                // Download the feed. 
                Uri feedUri = new Uri(URL);
                feed = await client.RetrieveFeedAsync(feedUri);
                isValid = true;

            }
            catch (Exception)
            {
                //If an error occured getting feed data, the feed is non-valid.
                isValid = false;
            }

            return feed;
        }



        //Build a tile XML from the template we have and the feed's items.
        private XmlDocument buildTileXML(SyndicationFeed feed)
        {
            String cmplteTile = null;
            ResourceLoader rl = new ResourceLoader();

            if (feed == null)
            {
                //Return a bogus tile saying the feed is not valid. 
                cmplteTile = rl.GetString("ErrorTileXML");
            }
            else
            {
                // Create a tile update manager for the specified syndication feed.
                var updater = TileUpdateManager.CreateTileUpdaterForApplication();
                updater.EnableNotificationQueue(true);
                updater.Clear();

                // Keep track of the number feed items that get tile notifications. 
                int itemCount = 0;

                //Create tiles
                XmlDocument tileXml = new Windows.Data.Xml.Dom.XmlDocument();
                tileXml.LoadXml(customXml);

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

                    string titleDesc = desc == null ? String.Empty : desc.Text;
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

                cmplteTile = tileXml.GetXml();

            }
           
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
            XmlDocument finalXml = new Windows.Data.Xml.Dom.XmlDocument();
            finalXml.LoadXml(cmplteTile);

            return finalXml;

        }

        //Try loading the custom XML as a Tile to see if it's valid.
        public Boolean testTileXML()
        {
            try
            {
                XmlDocument tileXml = new Windows.Data.Xml.Dom.XmlDocument();
                tileXml.LoadXml(customXml); //if we can't load the XML, it's probably not valid.
                TileNotification tileTest = new TileNotification(tileXml);

            }
            catch (Exception)
            {
                return false;
            }

            return true;

        }


        public string getTitle()
        {
            return feedTitle;
        }

        public void setTitle(string title)
        {
            feedTitle = title;
        }

        public string getURL()
        {
            return URL;
        }

        public string getTileXML()
        {
            return customXml;
        }

        public Boolean isTilePinned()
        {
            return isPinned;
        }

        public int getId()
        {
            return feedId;
        }

        public Boolean isTileValid()
        {
            return isValid;
        }

    }


}

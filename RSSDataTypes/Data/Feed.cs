using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Data.Json;
using Windows.Data.Xml.Dom;
using Windows.Foundation;
using Windows.UI.Notifications;
using Windows.UI.StartScreen;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Web.Syndication;

namespace RSSDataTypes.Data
{
    public sealed class Feed
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
            isValid = false;
        }

        public Feed(int i, string t, string u, string x)
        {
            feedId = i;
            feedTitle = t;
            URL = u;
            customXml = x;
            isValid = false;
        }

        public IAsyncOperation<bool> pinTileAsync()
        {
            Task<bool> load = pinTile();
            IAsyncOperation<bool> to = load.AsAsyncOperation();
            return to;
        }

        //Pins Tile to Start Menu.
        private async Task<bool> pinTile()
        {
            //Create the secondary tile
            string tileActivationArguments = feedId.ToString(); //We put the feed's ID as the activation argument => clicking on a sec.tile will open the app with that ID as argument
            string displayName = "RSS Live Tile for "+feedTitle;

            // Prepare package images for our tile to be pinned 
            Uri square150x150Logo = new Uri("ms-appx:///Assets/Square150x150Logo.scale-200.png");
            Uri wide310x150Logo = new Uri("ms-appx:///Assets/Wide310x150Logo.scale-200.png");
            Uri square310x310Logo = new Uri("ms-appx:///Assets/Square310x310Logo.scale-200.png");
            Uri square44x44Logo = new Uri("ms-appx:///Assets/Square44x44Logo.scale-200.png");
            
            TileSize newTileDesiredSize = TileSize.Square150x150;

            //The Secondary Tile unique ID is the Feed ID, makes checking easy.
            SecondaryTile secondaryTile = new SecondaryTile(feedId.ToString(),
                                                            displayName,
                                                            tileActivationArguments,
                                                            square150x150Logo,
                                                            newTileDesiredSize);

            //Setting visual elements
            secondaryTile.VisualElements.ShowNameOnSquare150x150Logo = false;
            secondaryTile.VisualElements.ShowNameOnWide310x150Logo = true;
            secondaryTile.VisualElements.ShowNameOnSquare310x310Logo = true;

            secondaryTile.VisualElements.Wide310x150Logo = wide310x150Logo;
            secondaryTile.VisualElements.Square310x310Logo = square310x310Logo;
            secondaryTile.VisualElements.Square44x44Logo = square44x44Logo;
            
            secondaryTile.VisualElements.ForegroundText = ForegroundText.Light;
            
            await secondaryTile.RequestCreateForSelectionAsync(new Windows.Foundation.Rect());


            //Save the unique tile ID and immediately try updating it with the XML we have
            await updateTile();

            return true;
        }

        //Unpins Tile from Start Menu.
        public async void unpinTile()
        {
            SecondaryTile secondaryTile = new SecondaryTile(feedId.ToString());

            if (isTilePinned())
                await secondaryTile.RequestDeleteForSelectionAsync(new Windows.Foundation.Rect());

        }

        public IAsyncOperation<bool> updateTileAsync()
        {
            Task<bool> load = updateTile();
            IAsyncOperation<bool> to = load.AsAsyncOperation();
            return to;
        }

        //Updates this feed's live tile, if it exists.
        private async Task<bool> updateTile()
        {
            SyndicationFeed feedData = await getFeedData();
            XmlDocument tileXml = await buildTileXML(feedData);
            TileNotification tileNotification = new TileNotification(tileXml);

            TileUpdater secondaryTileUpdater = TileUpdateManager.CreateTileUpdaterForSecondaryTile(feedId.ToString());
            secondaryTileUpdater.Update(tileNotification);

            return true;
        }

        public IAsyncOperation<string> getFeedTitleAsync()
        {
            Task<string> load = getFeedTitle();
            IAsyncOperation<string> to = load.AsAsyncOperation();
            return to;
        }

        //Get the RSS Feed's title, or the URL if it doesn't have one.
        private async Task<String> getFeedTitle()
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

        public IAsyncOperation<SyndicationFeed> getFeedDataAsync()
        {
            Task<SyndicationFeed> load = getFeedData();
            IAsyncOperation<SyndicationFeed> to = load.AsAsyncOperation();
            return to;
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

        //Returns the main domain of the feed. Used for favicon retrieval.
        public string getFeedDomain()
        {
            return new Uri(URL).Host;
        }

        public IAsyncOperation<string> getHiResFaviconAsync()
        {
            Task<string> load = getHiResFavicon();
            IAsyncOperation<string> to = load.AsAsyncOperation();
            return to;
        }

        //Tries getting a higher res favicon through the use of icons.better-idea.org. Falls back to Google S2 if there are no hi-res images.
        private async Task<string> getHiResFavicon()
        {
            var client = new HttpClient();

            //Before anything, we see if this Feed has an atom:icon. If it does, we return that, as it's made especially for the feed.
            //Commented out - It works, but most atom:icons are rectangular, which isn't a format suited for live tiles.
            /*SyndicationFeed f = await getFeedData();

            if (f.ImageUri != null)
                return f.ImageUri.ToString();
                */

            //If it doesn't, we look at the favicons for its domain.
            try
            {
                HttpResponseMessage response = await client.GetAsync(new Uri("https://icons.better-idea.org/allicons.json?url=" + getFeedDomain() + "&formats=png"));
                var jsonString = await response.Content.ReadAsStringAsync();

                JsonArray icons = JsonValue.Parse(jsonString).GetObject().GetNamedArray("icons");
                
                double largestWidth = 0;
                string urlLargestIcon = "http://www.google.com/s2/favicons?domain_url=" + getFeedDomain(); //Fallback in case there are no png icons available

                //Iterate on the icons array to get the image with the highest resolution
                foreach (JsonValue icon in icons)
                {
                    JsonObject obj = icon.GetObject();
                    if (obj.GetNamedNumber("width") > largestWidth)
                    {
                        largestWidth = obj.GetNamedNumber("width");
                        urlLargestIcon = obj.GetNamedString("url");
                    }
                }

                return urlLargestIcon;
            }
            catch (Exception e)
            {
                return "http://www.google.com/s2/favicons?domain_url=" + getFeedDomain();
            }

        }

        //Build a tile XML from the template we have and the feed's items.
        private async Task<XmlDocument> buildTileXML(SyndicationFeed feed)
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

                //grab Favicon and insert its url in the XML where the #favicon# tag is
                String xmlBeforeDataInsertion = customXml;
                String faviconURL = await getHiResFavicon();  
                xmlBeforeDataInsertion = 
                    xmlBeforeDataInsertion.Replace("#favicon#", faviconURL); //low effort strikes again

                tileXml.LoadXml(xmlBeforeDataInsertion);

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
                    titleText = titleText.Replace(System.Environment.NewLine, ""); //Strip newlines from titles for easier reading

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
            return SecondaryTile.Exists(feedId.ToString());
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

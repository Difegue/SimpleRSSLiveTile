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
using HtmlAgilityPack;

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
        [JsonProperty] private bool useAtomIcon;

        public Feed()
        {
        }

        //
        // Summary:
        //     Create a new Feed, storable in FeedDataSource.
        //
        // Parameters:
        //   i:
        //     ID of the Feed.
        //   t:
        //     Title of the Feed.
        //   u: 
        //     URL of the Feed.
        public Feed(int i, string t, string u)
        {
            ResourceLoader rl = new ResourceLoader();

            feedId = i;
            feedTitle = t;
            URL = u;
            customXml = rl.GetString("AdaptiveTemplate");
            isValid = false;
            useAtomIcon = false;
        }

        //
        // Summary:
        //     Create a new Feed, storable in FeedDataSource, with custom XML.
        //
        // Parameters:
        //   i:
        //     ID of the Feed.
        //   t:
        //     Title of the Feed.
        //   u: 
        //     URL of the Feed.
        //   x:
        //     Custom XML for the Feed.
        public Feed(int i, string t, string u, string x)
        {
            feedId = i;
            feedTitle = t;
            URL = u;
            customXml = x;
            isValid = false;
            useAtomIcon = false;
        }

        public IAsyncOperation<bool> PinTileAsync()
        {
            Task<bool> load = PinTile();
            IAsyncOperation<bool> to = load.AsAsyncOperation();
            return to;
        }

        //Unpins Tile from Start Menu.
        public async void UnpinTileAsync()
        {
            SecondaryTile secondaryTile = new SecondaryTile(feedId.ToString());

            if (IsTilePinned())
                await secondaryTile.RequestDeleteForSelectionAsync(new Windows.Foundation.Rect());

        }

        public IAsyncOperation<bool> UpdateTileAsync()
        {
            Task<bool> load = UpdateTile();
            IAsyncOperation<bool> to = load.AsAsyncOperation();
            return to;
        }


        public IAsyncOperation<string> GetFeedTitleAsync()
        {
            Task<string> load = GetFeedTitle();
            IAsyncOperation<string> to = load.AsAsyncOperation();
            return to;
        }

        public IAsyncOperation<SyndicationFeed> GetFeedDataAsync()
        {
            Task<SyndicationFeed> load = GetFeedData();
            IAsyncOperation<SyndicationFeed> to = load.AsAsyncOperation();
            return to;
        }

        //Returns the main domain of the feed. Used for favicon retrieval.
        public string GetFeedDomain()
        {
            return new Uri(URL).Host;
        }

        public IAsyncOperation<string> getHiResFaviconAsync()
        {
            Task<string> load = GetHiResFaviconAsync();
            IAsyncOperation<string> to = load.AsAsyncOperation();
            return to;
        }

        //Try loading the custom XML as a Tile to see if it's valid.
        public Boolean TestTileXML()
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

        public string GetTitle()
        {
            return feedTitle;
        }

        public void SetTitle(string title)
        {
            feedTitle = title;
        }

        public string GetURL()
        {
            return URL;
        }

        public string GetTileXML()
        {
            return customXml;
        }

        public Boolean IsTilePinned()
        {
            return SecondaryTile.Exists(feedId.ToString());
        }

        public int GetId()
        {
            return feedId;
        }

        public Boolean IsTileValid()
        {
            return isValid;
        }

        public void SetAtomIconUse(bool b)
        {
            useAtomIcon = b;
        }

        public bool IsUsingAtomIcon()
        {
            return useAtomIcon;
        }


        //Pins Tile to Start Menu.
        private async Task<bool> PinTile()
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
            await UpdateTile();

            return true;
        }


        //Updates this feed's live tile, if it exists.
        private async Task<bool> UpdateTile()
        {
            SyndicationFeed feedData = await GetFeedData();
            XmlDocument tileXml = await BuildTileXMLAsync(feedData);
            TileNotification tileNotification = new TileNotification(tileXml);

            TileUpdater secondaryTileUpdater = TileUpdateManager.CreateTileUpdaterForSecondaryTile(feedId.ToString());
            secondaryTileUpdater.Update(tileNotification);

            return true;
        }

        //Get the RSS Feed's title, or the URL if it doesn't have one.
        private async Task<String> GetFeedTitle()
        {

            SyndicationFeed feed = await GetFeedData();
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
        private async Task<SyndicationFeed> GetFeedData()
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

        //Tries getting a higher res favicon through the use of icons.better-idea.org. Falls back to Google S2 if there are no hi-res images.
        private async Task<string> GetHiResFaviconAsync()
        {
            var client = new HttpClient();

            //Before anything, we see if this Feed has an atom:icon. If it does, we return that, as it's made especially for the feed.
            SyndicationFeed f = await GetFeedData(); //Might return null for invalid feeds - we check for that down below

            if (f != null && f.ImageUri != null && useAtomIcon)
                return f.ImageUri.ToString();

            //If it doesn't, we look at the favicons for its domain.
            try
            {
                //Use the statvoo API for higher res favicons -- but check if it's alive first
                HttpResponseMessage response = await client.GetAsync(new Uri("https://api.statvoo.com/favicon/?url=" + GetFeedDomain()));
                return "https://api.statvoo.com/favicon/?url=" + GetFeedDomain();
            }
            catch (Exception)
            {
                //Google S2 is a great fallback but only offers 16x16 images.
                return "http://www.google.com/s2/favicons?domain_url=" + GetFeedDomain();
            }

        }

        //Build a tile XML from the template we have and the feed's items.
        private async Task<XmlDocument> BuildTileXMLAsync(SyndicationFeed feed)
        {
            String cmplteTile = null;
            ResourceLoader rl = new ResourceLoader();

            String[] imageForItem = new String[4];
            for (int i = 0; i < 4; i++)
                imageForItem[i] = "";

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

                // Keep track of the number of feed items that get tile notifications. 
                int itemCount = 0;

                //Create tiles
                XmlDocument tileXml = new Windows.Data.Xml.Dom.XmlDocument();

                //grab Favicon and insert its url in the XML where the #favicon# tag is
                String xmlBeforeDataInsertion = customXml;
                String faviconURL = await GetHiResFaviconAsync();
                xmlBeforeDataInsertion =
                    xmlBeforeDataInsertion.Replace("#favicon#", faviconURL); //low effort strikes again

                try {
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
                        string titleDesc = desc == null ? String.Empty : desc.Text;
                        
                        titleText = System.Net.WebUtility.HtmlDecode(titleText);
                        titleDesc = System.Net.WebUtility.HtmlDecode(titleDesc);
                        
                        //Try getting an image for this item.
                        string imgUrl = GetImageFromItem(titleDesc);
                        if (imgUrl == "")
                        {
                            //If we couldn't find an image in the Summary, we look in the Content as well before giving up.
                            var content = item.Content;

                            string titleContent = content == null ? String.Empty : content.Text;
                            titleContent = System.Net.WebUtility.HtmlDecode(titleContent);
                            imgUrl = GetImageFromItem(titleContent);
                        }
                        imageForItem[itemCount] = imgUrl;


                        //Strip all XML/HTML tags.
                        HtmlDocument doc = new HtmlDocument();
                        doc.LoadHtml(titleText);
                        titleText = doc.DocumentNode.InnerText;
                        doc.LoadHtml(titleDesc);
                        titleDesc = doc.DocumentNode.InnerText;

                        titleText = titleText.Replace(System.Environment.NewLine, ""); //Strip newlines from titles for easier reading

                        if (titleText.Length > 150) //A tile can't show more than 134 characters on a line (TileWide), so we limit each item to 150 chars. Also helps keeping the xml payload under 5kb.
                            titleText = titleText.Substring(0, 150);


                        if (titleDesc.Length > 150)
                            titleDesc = titleDesc.Substring(0, 150);

                        XmlNodeList nodeListTitle = tileXml.GetElementsByTagName(textElementName[itemCount]);
                        XmlNodeList nodeListDesc = tileXml.GetElementsByTagName(descElementName[itemCount]);

                        foreach (IXmlNode node in nodeListTitle)
                            node.InnerText = titleText;

                        foreach (IXmlNode node in nodeListDesc)
                            node.InnerText = titleDesc;

                        // Don't get more than 4 items.
                        itemCount++;
                        if (itemCount > 3)
                            break;
                    }

                    cmplteTile = tileXml.GetXml();
                }
                catch (Exception)
                {
                    //Return the error tile if we get an exception during treatment (likely caused by the initial LoadXML)
                    cmplteTile = rl.GetString("ErrorTileXML");
                }

            }

            //The tags used to insert the RSS elements in the XML need to be removed for the tile to properly appear in the Start Menu.
            //We also replace the image tags here with the URLs we obtained while parsing the feed items.
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

            for (int i = 0; i<4; i++)
                cmplteTile = cmplteTile.Replace("#img"+(i+1)+"#", imageForItem[i]);

            //Reload the XML after corrections have been made
            XmlDocument finalXml = new Windows.Data.Xml.Dom.XmlDocument();
            try
            {
                finalXml.LoadXml(cmplteTile);
            }
            catch (Exception)
            {
                //Display the error XML if loading our finalized tile fails. Avoids the 800 or so failures showing in my Windows Store report right now.  (・△・') 
                cmplteTile = rl.GetString("ErrorTileXML");
                finalXml.LoadXml(cmplteTile);
            }

            return finalXml;

        }

        //Use HtmlAgilityPack to find the first <img> tag, and return its src attribute.
        private string GetImageFromItem(string html)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            string ret;

            try
            {
                HtmlAttribute att = doc.DocumentNode.ChildNodes.FindFirst("img").Attributes["src"];
                ret = att.Value;
            }
            catch (Exception)
            {
                ret = "";
            }

            return ret;
        }

    }


}

using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Web.Syndication;

namespace RSSDataTypes.Data
{
    /// <summary>
    /// Simple Class for representing a Feed's articles.
    /// </summary>
    public sealed class Article
    {

        [JsonProperty] public string Title { get; set; }
        [JsonProperty] public Uri URL { get; set; }
        [JsonProperty] public string Summary { get; set; }
        [JsonProperty] public DateTimeOffset PublishedDate { get; set; }
        [JsonProperty] public string PublishedDateFormatted => PublishedDate.ToString("dd/MM/yyyy    h:mm tt").ToUpper();
        [JsonProperty] public string ImageURL { get; set; }

        public Article()
        {
            Title = "Feed";
            Summary = "";
            URL = new Uri("http://example.com");
            ImageURL = "";
            PublishedDate = new DateTimeOffset(DateTime.Now);
        }

        public Article(SyndicationItem item, Uri baseUri)
        {
            if (item.Title != null)
                Title = item.Title.Text;
            if (item.Summary != null)
                Summary = item.Summary.Text;

            //Try getting an image for this item.
            ImageURL = GetImageFromItem(Summary);
            if (ImageURL == "")
            {
                //If we couldn't find an image in the Summary, we look in the Content as well before giving up.
                var content = item.Content;

                string titleContent = content == null ? String.Empty : content.Text;
                titleContent = System.Net.WebUtility.HtmlDecode(titleContent);
                ImageURL = GetImageFromItem(titleContent);
            }

            //Strip all XML/HTML tags.
            HtmlDocument doc = new HtmlDocument();
            if (Title != null)
            {
                doc.LoadHtml(Title);
                Title = doc.DocumentNode.InnerText;
            }

            if (Summary != null)
            {
                doc.LoadHtml(Summary);
                Summary = doc.DocumentNode.InnerText;
            }

            Title = Title.Replace(System.Environment.NewLine, ""); //Strip newlines from titles for easier reading

            PublishedDate = item.PublishedDate;
            URL = baseUri;
        }

        //Use HtmlAgilityPack to find the first <img> tag, and return its src attribute.
        private string GetImageFromItem(string html)
        {
            string ret;

            try
            {
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(html);
                
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

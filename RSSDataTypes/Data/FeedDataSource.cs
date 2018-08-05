using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;

namespace RSSDataTypes.Data
{
    public sealed class FeedDataSource
    {
        private ApplicationDataContainer feedDB;
        private StorageFolder articleCache;

        //Binds to the App DataStorage to get Feeds easily. Handles translation from the Feed object to a Composite Storage Value.
        public FeedDataSource()
        {
            feedDB = ApplicationData.Current.LocalSettings;
            articleCache = ApplicationData.Current.LocalCacheFolder;
        }

        public bool FeedExists(string feedId)
        {
            if (feedId != null && feedId != "")
                return feedDB.Values.Keys.Contains(feedId);

            return false;
        }

        public IList<Feed> GetAllFeeds()
        {

            IList<Feed> allFeeds = new List<Feed>();

            foreach (String feedID in feedDB.Values.Keys)
            {
                try
                {
                    int id = int.Parse(feedID);
                    String feedJSON = (String)feedDB.Values[feedID];

                    Feed f = JsonConvert.DeserializeObject<Feed>(feedJSON);

                    allFeeds.Add(f);
                }
                catch (Exception)
                {
                    //If we couldn't deserialize a Feed from this ID, it's not a Feed. 
                    //If the feedID is invalid (ergo not an int), remove the matching value from the storage
                    feedDB.Values.Remove(feedID);
                }
            }

            return allFeeds;
        }

        public void SetFeed(Feed f)
        {
            //Save feed to LocalSettings
            String feedJSON = JsonConvert.SerializeObject(f);
            feedDB.Values[f.GetId().ToString()] = feedJSON;
        }

        public Feed GetFeedById(int id)
        {
            String feedJSON = (String)feedDB.Values[id.ToString()];

            if (feedJSON != null)
            {
                Feed f = JsonConvert.DeserializeObject<Feed>(feedJSON);
                return f;
            }

            //high-level meta joke
            return new Feed(id, "Something happened", "http://example.com");
        }

        public IAsyncOperation<bool> DeleteFeed(int id)
        {
            Task<bool> load = DeleteFeedInternal(id);
            IAsyncOperation<bool> to = load.AsAsyncOperation();
            return to;
        }

        private async Task<bool> DeleteFeedInternal(int id)
        {
            //Delete matching CacheFolder file
            IStorageItem file = await articleCache.TryGetItemAsync(id.ToString() + ".articlecache");

            if (file != null)
                await file.DeleteAsync();

            return feedDB.Values.Remove(id.ToString());
        }

        public IAsyncOperation<bool> SaveCachedArticles(Feed f, IList<Article> articleList)
        {
            Task<bool> load = SaveCachedArticlesInternal(f, articleList);
            IAsyncOperation<bool> to = load.AsAsyncOperation();
            return to;
        }

        private async Task<bool> SaveCachedArticlesInternal(Feed f, IList<Article> articleList)
        {
            //Save articles to CacheFolder
            string articlesJSON = JsonConvert.SerializeObject(articleList, Formatting.Indented);
            StorageFile articlesFile = await articleCache.CreateFileAsync(f.GetId().ToString() + ".articlecache",
                                        CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(articlesFile, articlesJSON);

            return true;

        }

        public IAsyncOperation<IList<Article>> GetCachedArticles(Feed f)
        {
            Task<IList<Article>> load = GetCachedArticlesInternal(f);
            IAsyncOperation<IList<Article>> to = load.AsAsyncOperation();
            return to;
        }

        private async Task<IList<Article>> GetCachedArticlesInternal(Feed f)
        {
            //Check if there is a cache first
            IStorageItem file = await articleCache.TryGetItemAsync(f.GetId().ToString() + ".articlecache");

            if (file == null)
                return new List<Article>();

            //Get JSON from cachedFolder and unserialize it into the Article list
            StorageFile cachedArticles = await articleCache.GetFileAsync(f.GetId().ToString() + ".articlecache");
            String fileContent = await FileIO.ReadTextAsync(cachedArticles);
            
            return JsonConvert.DeserializeObject<List<Article>>(fileContent);

        }

    }
}

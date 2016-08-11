using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackgroundTasks.Data
{
    class FeedDataSource
    {
        private Windows.Storage.ApplicationDataContainer feedDB; 

        //Binds to the App DataStorage to get Feeds easily. Handles translation from the Feed object to a Composite Storage Value.

        public FeedDataSource()
        {
            feedDB = Windows.Storage.ApplicationData.Current.LocalSettings;
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
                catch (Exception e)
                {
                    //If the feedID is invalid (ergo not an int), remove the matching value from the storage
                    feedDB.Values.Remove(feedID);
                }
            }

            return allFeeds;
        }

        public void SetFeed(Feed f)
        {

            String feedJSON = JsonConvert.SerializeObject(f);

            feedDB.Values[f.getId().ToString()] = feedJSON;

        }

        public Feed GetFeedById(int id)
        {
            String feedJSON = (String)feedDB.Values[id.ToString()];

            return JsonConvert.DeserializeObject<Feed>(feedJSON);
        }

        public bool DeleteFeed(int id)
        {
            return feedDB.Values.Remove(id.ToString());
        }

    }
}

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
using RSSDataTypes.Data;

namespace BackgroundTasks
{

    //Background Task Version.
    public sealed class RSSFeedBackgroundTask : IBackgroundTask
    {
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            // Get a deferral, to prevent the task from closing prematurely 
            // while asynchronous code is still running.
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();

            await updatePinnedTiles();

            // Inform the system that the task is finished.
            deferral.Complete();
        }

        private async Task updatePinnedTiles()
        {
            //Wow it's fucking nothing
            FeedDataSource feedDB = new FeedDataSource();
            foreach (Feed f in feedDB.GetAllFeeds())
            {
                if (f.isTilePinned())
                    await f.updateTileAsync();
            }
        }
    }
    
}


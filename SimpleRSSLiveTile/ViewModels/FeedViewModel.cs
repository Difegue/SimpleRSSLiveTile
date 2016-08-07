using SimpleRSSLiveTile.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;

namespace SimpleRSSLiveTile.ViewModels
{
    public class FeedViewModel: INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string URL { get; set; }
        public string TileXML { get; set; }
        public string FaviconURL { get; set; }

        public FeedViewModel()
        {
            Title = "Feed";
            URL = "FeedURL";
            FaviconURL = "http://www.google.com/s2/favicons?domain_url=http://google.com";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public static FeedViewModel FromFeed(Feed item)
        {
            var viewModel = new FeedViewModel();

            viewModel.Id = item.getId();
            viewModel.Title = item.getTitle();
            viewModel.URL = item.getURL();
            viewModel.TileXML = item.getTileXML();
            viewModel.FaviconURL = "http://www.google.com/s2/favicons?domain_url=" + item.getURL(); //Low effort

            return viewModel;

        }

        // PropertyChanged event triggering method.
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

    }
}

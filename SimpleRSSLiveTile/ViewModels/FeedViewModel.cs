using RSSDataTypes.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        public string FaviconHiResImage { get; set; }
        public bool usingAtomIcon { get; set; }
        public ObservableCollection<Article> Articles { get; set; }

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

            viewModel.Id = item.GetId();
            viewModel.Title = item.GetTitle();
            viewModel.URL = item.GetURL();
            viewModel.TileXML = item.GetTileXML();
            viewModel.FaviconURL = "http://www.google.com/s2/favicons?domain_url=" + item.GetFeedDomain(); //Low effort
            viewModel.usingAtomIcon = item.IsUsingAtomIcon();
            viewModel.Articles = new ObservableCollection<Article>();

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

using RSSDataTypes.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;

namespace SimpleRSSLiveTile
{
    //Stores a reference to the current master page in order for subpages to call its public methods, namely updateFeedList. 
    //The master page updates the reference by itself when it's loaded.
    //This is absolutely disgusting as far as best practices go I guess
    public static class GlobalMainPageReference
    {
        public static NewMainPage mainPage;
    }
}

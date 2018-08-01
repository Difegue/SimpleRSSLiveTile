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

using System;
using System.Text.RegularExpressions;

namespace wget.Domain
{
    public class SpiderManager
    {
        public void Crawl(string startUrl, string target)
        {
            Uri uri = new Uri(startUrl);          
            ProgressReporter progressReporter = new ProgressReporter();
            DownLoader downLoader = new DownLoader(new Uri(startUrl), startUrl, target, progressReporter);
            downLoader.Get();
        }
    }
}
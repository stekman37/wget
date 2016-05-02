using System;
using System.Collections.Generic;
using System.Threading;

namespace wget.SiteDownloader
{
    public class DownloadCommander
    {
        private readonly Mutex _setMutex = new Mutex();
        private readonly ISet<Uri> _uris = new HashSet<Uri>();

        public void FileCompleteReport(Uri uri, string target)
        {
            Console.WriteLine($"{uri} => {target} (Downloaded)");
        }

        public bool PermissionToStart(Uri uri)
        {
            _setMutex.WaitOne();
            bool isNewUri;
            try
            {
                if (_uris.Contains(uri))
                {
                    isNewUri = false;
                }
                else
                {
                    isNewUri = true;
                    _uris.Add(uri);
                }
            }
            finally
            {
                _setMutex.ReleaseMutex();
            }
            return isNewUri;
        }

        public void ProgressReport(Uri uri, int progressPercentage)
        {
            Console.WriteLine($"{uri} ({progressPercentage}%)");
        }

        public void StartCrawl(string startUrl, string target)
        {
            var siteUri = new Uri(startUrl);
            var targetUri = new Uri(target);
            var fileDownLoader = new FileDownLoader(new ParseItem(siteUri, new Uri(targetUri, "index.html")), siteUri,
                targetUri, this);
            fileDownLoader.Get();
        }
    }
}
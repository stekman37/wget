using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wget.SiteDownloader;

namespace wget
{
    class Program
    {
        static void Main(string[] args)
        {
            var target = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/test/";
            var startUrl = @"http://www.vergic.com/";
            var downloadCommander = new DownloadCommander();
            downloadCommander.StartCrawl(startUrl, target);
        }
    }
}

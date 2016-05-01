using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wget.Domain;

namespace wget
{
    class Program
    {
        static void Main(string[] args)
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/test/";
            var start = @"http://www.vergic.com/";
            var spiderManager = new SpiderManager();
            spiderManager.Crawl(start, path);
            Console.ReadLine();
        }
    }
}

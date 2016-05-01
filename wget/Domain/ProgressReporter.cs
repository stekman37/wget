using System;
using System.Collections.Generic;
using System.Threading;

namespace wget.Domain
{
    public class ProgressReporter
    {
        private ISet<Uri> uris = new HashSet<Uri>();
        private readonly Mutex _setMutex = new Mutex();
        public void PercentComplete(Uri uri, int progressPercentage)
        {
            Console.WriteLine($"{uri} ({progressPercentage}%");
        }

        public bool StartGet(Uri uri)
        {
            _setMutex.WaitOne();
            bool isNewUri;
            try
            {
                if (uris.Contains(uri))
                {
                    isNewUri = false;
                }
                else
                {
                    isNewUri = true;
                    uris.Add(uri);
                }
            }
            finally
            {
                _setMutex.ReleaseMutex();
            }
            return isNewUri;
        }

        public void FileComplete(Uri uri, string target)
        {
            Console.WriteLine($"{uri} => {target} (Downloaded)");
        }
    }
}
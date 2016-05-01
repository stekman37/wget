using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Handlers;
using System.Text.RegularExpressions;

namespace wget.Domain
{
    public class DownLoader
    {
        private readonly ProgressMessageHandler _progressHandler = new ProgressMessageHandler();

        private Uri _uri;
        private readonly string _baseUri;
        private readonly string _target;
        private readonly ProgressReporter _progressReporter;

        public DownLoader(Uri uri, string baseUri, string target, ProgressReporter progressReporter)
        {
            _uri = uri;
            _baseUri = baseUri;
            _target = target;
            _progressReporter = progressReporter;
        }

        public async void Get()
        {
            var client = GetClient();
            var response = await client.GetAsync(_uri);
            if (response.IsSuccessStatusCode)
            {
                var mimeType = response.Content.Headers.ContentType.MediaType;
                var filename = Filename(mimeType);
                if (IsHtml(mimeType))
                {
                    var html = await response.Content.ReadAsStringAsync();
                    new FileInfo(filename).Directory?.Create();
                    File.WriteAllText(filename, html);
                    PageParser parser = new PageParser(_baseUri);
                    var urls = parser.FindUrls(html);
                    foreach (var url in urls)
                    {
                        Uri newUri = new Uri(url);
                        Uri uri;
                        uri = newUri.IsAbsoluteUri ? newUri : new Uri(_uri, url);
                        if (_progressReporter.StartGet(uri))
                        {
                            var downloader = new DownLoader(uri, _baseUri, _target,_progressReporter);
                            downloader.Get();
                        }
                    }
                }
                else
                {                 
                    var byteArray = await response.Content.ReadAsByteArrayAsync();
                    new FileInfo(filename).Directory?.Create();
                    File.WriteAllBytes(filename, byteArray);                            
                }
                _progressReporter.FileComplete(_uri, _target);
            }
            else if (response.StatusCode == HttpStatusCode.Redirect)
            {
                _uri = response.Content.Headers.ContentLocation;
                if (_progressReporter.StartGet(_uri))
                {
                    var downloader = new DownLoader(_uri, _baseUri, _target, _progressReporter);
                    downloader.Get();
                }
            }
        }

        private static bool IsHtml(string mimeType)
        {
            return mimeType.StartsWith("text/htm") || mimeType.StartsWith("application/xhtml");
        }

        private string Filename(string mimeType)
        {
            var localpath = _uri.LocalPath;
            var rgx= new Regex(@"^(.*)/([^/\.]*\.[^/\.]*)$");
            var match = rgx.Match(localpath);
            string filename;
            string extension;
            if (!match.Success)
            {
                if (IsHtml(mimeType))
                {
                    filename = localpath+"/index";
                    extension = "html";
                }
                else
                {
                    filename = localpath+"/unknown";
                    extension = "bin";
                }
            }
            else
            {
                filename = match.Groups[1].Value;
                extension = match.Groups[2].Value;
            }
            var query = _uri.Query;
            localpath = $"{filename}/{query.GetHashCode()}.{extension}";
            var authority = _uri.Authority;
            var path = $"{_target}/{authority}/{localpath}";
            return path;
        }

        private HttpClient GetClient()
        {
            var handlers = new List<DelegatingHandler>(1) {_progressHandler};
            _progressHandler.HttpReceiveProgress += OnHttpProgress;
            var client = HttpClientFactory.Create(handlers.ToArray());
            return client;
        }

        private void OnHttpProgress(object sender, HttpProgressEventArgs e)
        {
            _progressReporter.PercentComplete(_uri, e.ProgressPercentage);
        }
    }
}
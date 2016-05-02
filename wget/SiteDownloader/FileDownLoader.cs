using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Handlers;
using System.Threading.Tasks;

namespace wget.SiteDownloader
{
    public class FileDownLoader
    {
        private readonly Uri _baseSiteUri;
        private readonly DownloadCommander _downloadCommander;
        private readonly ParseItem _parseItem;
        private readonly ProgressMessageHandler _progressHandler = new ProgressMessageHandler();
        private readonly Uri _targetFolder;

        public FileDownLoader(ParseItem parseItem, Uri baseSiteUri, Uri targetFolder,
            DownloadCommander downloadCommander)
        {
            _parseItem = parseItem;
            _baseSiteUri = baseSiteUri;
            _targetFolder = targetFolder;
            _downloadCommander = downloadCommander;
        }

        public async void Get()
        {
            var client = GetClient();
            var response = await client.GetAsync(_parseItem.SiteUri);
            var filename = new Uri(_targetFolder, _parseItem.FileUri).LocalPath;
            if (response.IsSuccessStatusCode)
            {
                var mimeType = response.Content.Headers.ContentType.MediaType;
                if (IsHtml(mimeType))
                {
                    await GetHtmlAndLinked(response, filename);
                }
                else if (IsCss(mimeType))
                {
                    await GetCssAndLinked(response, filename);
                }
                else if (IsScript(mimeType))
                {
                    await GetScriptAndLinked(response, filename);
                }
                else
                {
                    await WriteFile(response, filename);
                }
                _downloadCommander.FileCompleteReport(_parseItem.SiteUri, filename);
            }
            else if (response.StatusCode == HttpStatusCode.Redirect)
            {
                _parseItem.SiteUri = response.Content.Headers.ContentLocation;
                if (_downloadCommander.PermissionToStart(_parseItem.SiteUri))
                {
                    var downloader = new FileDownLoader(_parseItem, _baseSiteUri, _targetFolder, _downloadCommander);
                    downloader.Get();
                }
            }
        }

        private void DownloadLinked(ParseResult parseResult)
        {
            foreach (var parseItem in parseResult.Uris)
            {
                if (_downloadCommander.PermissionToStart(parseItem.SiteUri))
                {
                    var downloader = new FileDownLoader(parseItem, _baseSiteUri, _targetFolder, _downloadCommander);
                    downloader.Get();
                }
            }
        }


        private HttpClient GetClient()
        {
            var handlers = new List<DelegatingHandler>(1) {_progressHandler};
            _progressHandler.HttpReceiveProgress += OnHttpProgress;
            var client = HttpClientFactory.Create(handlers.ToArray());
            return client;
        }

        private async Task GetCssAndLinked(HttpResponseMessage response, string filename)
        {
            var css = await response.Content.ReadAsStringAsync();
            var parser = new PageParser(_baseSiteUri, _parseItem.SiteUri);
            var parseResult = parser.AnalyzeCss(css);
            WriteFile(filename, parseResult);
            DownloadLinked(parseResult);
        }

        private async Task GetHtmlAndLinked(HttpResponseMessage response, string filename)
        {
            var html = await response.Content.ReadAsStringAsync();
            var parser = new PageParser(_baseSiteUri, _parseItem.SiteUri);
            var parseResult = parser.AnalyzeHtml(html);
            WriteFile(filename, parseResult);
            DownloadLinked(parseResult);
        }

        private async Task GetScriptAndLinked(HttpResponseMessage response, string filename)
        {
            var script = await response.Content.ReadAsStringAsync();
            var parser = new PageParser(_baseSiteUri, _parseItem.SiteUri);
            var parseResult = parser.AnalyzeScript(script);
            WriteFile(filename, parseResult);
            DownloadLinked(parseResult);
        }

        private static bool IsCss(string mimeType)
        {
            return mimeType.Equals("text/css");
        }

        private static bool IsHtml(string mimeType)
        {
            return mimeType.StartsWith("text/htm") || mimeType.StartsWith("application/xhtml");
        }

        private static bool IsScript(string mimeType)
        {
            return mimeType.Equals("application/x-javascript") || mimeType.Equals("application/javascript") ||
                   mimeType.Equals("application/ecmascript") || mimeType.Equals("text/ecmascript") ||
                   mimeType.Equals("text/javascript");
        }

        private void OnHttpProgress(object sender, HttpProgressEventArgs e)
        {
            _downloadCommander.ProgressReport(_parseItem.SiteUri, e.ProgressPercentage);
        }

        private static async Task WriteFile(HttpResponseMessage response, string filename)
        {
            var byteArray = await response.Content.ReadAsByteArrayAsync();
            new FileInfo(filename).Directory?.Create();
            File.WriteAllBytes(filename, byteArray);
        }

        private static void WriteFile(string filename, ParseResult parseResult)
        {
            new FileInfo(filename).Directory?.Create();
            File.WriteAllText(filename, parseResult.Payload);
        }
    }
}
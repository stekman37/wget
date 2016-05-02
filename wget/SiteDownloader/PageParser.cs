using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace wget.SiteDownloader
{
    public class PageParser
    {
        private readonly Uri _baseUri;
        private readonly Uri _uri;

        public PageParser(Uri baseUri, Uri uri)
        {
            _baseUri = baseUri;
            _uri = uri;
        }

        public ParseResult AnalyzeCss(string html)
        {
            var parseResult = new ParseResult();
            var rgx = new Regex(@"uri\((" + _baseUri + @")\)[^\b;]*");
            var matches = rgx.Match(html);
            while (matches.Success)
            {
                var uri = MakeUri(matches.Groups[1].Value);
                var filepath = MakeFileSystemFriendly(uri);
                parseResult.Uris.Add(new ParseItem(uri, filepath));
                matches.NextMatch();
            }
            html = html.Replace(_baseUri.ToString(), "");
            parseResult.Payload = html;
            return parseResult;
        }

        public ParseResult AnalyzeHtml(string page)
        {
            var htmlDoc = new HtmlDocument {OptionFixNestedTags = true};
            htmlDoc.LoadHtml(page);
            var parseResult = new ParseResult();
            if (htmlDoc.DocumentNode != null)
            {
                FindLinks(htmlDoc, "href", parseResult);
                FindLinks(htmlDoc, "src", parseResult);
                FindCss(htmlDoc, parseResult);
                FindScript(htmlDoc, parseResult);
                var updatedHtml = new StringBuilder();
                var stringWriter = new StringWriter(updatedHtml);
                htmlDoc.DocumentNode.WriteTo(stringWriter);
                parseResult.Payload = updatedHtml.ToString();
            }
            return parseResult;
        }

        public ParseResult AnalyzeScript(string html)
        {
            var parseResult = new ParseResult();
            var rgx = new Regex(@"""(" + _baseUri + @")[^""]*");
            var matches = rgx.Match(html);
            while (matches.Success)
            {
                var uri = MakeUri(matches.Groups[1].Value);
                var filepath = MakeFileSystemFriendly(uri);
                parseResult.Uris.Add(new ParseItem(uri, filepath));
                matches.NextMatch();
            }
            html = html.Replace(_baseUri.ToString(), "");
            parseResult.Payload = html;
            return parseResult;
        }


        private void FindCss(HtmlDocument htmlDoc, ParseResult parseResult)
        {
            var rgx = new Regex(@"uri\((" + _baseUri + @")\)[^\b;]*");
            var htmlNodeCollection = htmlDoc.DocumentNode.SelectNodes($"//style");
            if (htmlNodeCollection != null)
            {
                foreach (var node in htmlNodeCollection)
                {
                    var matches = rgx.Match(node.InnerHtml);
                    while (matches.Success)
                    {
                        var uri = MakeUri(matches.Groups[1].Value);
                        var filepath = MakeFileSystemFriendly(uri);
                        parseResult.Uris.Add(new ParseItem(uri, filepath));
                        matches.NextMatch();
                    }
                    node.InnerHtml = node.InnerHtml.Replace(_baseUri.ToString(), "");
                }
            }
        }


        private void FindLinks(HtmlDocument htmlDoc, string attribute, ParseResult parseResult)
        {
            var htmlNodeCollection = htmlDoc.DocumentNode.SelectNodes($"//*/@{attribute}");
            if (htmlNodeCollection != null)
            {
                foreach (var nodeWithLink in htmlNodeCollection)
                {
                    var v = nodeWithLink.Attributes[attribute].Value;
                    var link = MakeUri(v);
                    if (IsInSite(link))
                    {
                        var fileUri = MakeFileSystemFriendly(link);
                        parseResult.Uris.Add(new ParseItem(link, fileUri));
                        var absoluteUri = new Uri(_baseUri, fileUri);
                        var relativeUri = _uri.MakeRelativeUri(absoluteUri);
                        nodeWithLink.Attributes[attribute].Value = relativeUri.ToString();
                    }
                }
            }
        }

        private void FindScript(HtmlDocument htmlDoc, ParseResult parseResult)
        {
            var rgx = new Regex(@"""(" + _baseUri + @"[^""]*)""");
            var htmlNodeCollection = htmlDoc.DocumentNode.SelectNodes($"//script");
            if (htmlNodeCollection != null)
            {
                foreach (var node in htmlNodeCollection)
                {
                    var matches = rgx.Match(node.InnerHtml);
                    while (matches.Success)
                    {
                        var uri = MakeUri(matches.Groups[1].Value);
                        var filepath = MakeFileSystemFriendly(uri);
                        parseResult.Uris.Add(new ParseItem(uri, filepath));
                        matches.NextMatch();
                    }
                    node.InnerHtml = node.InnerHtml.Replace(_baseUri.ToString(), "");
                }
            }
        }

        private bool IsInSite(Uri link)
        {
            return _baseUri.IsBaseOf(link);
        }

        private Uri MakeFileSystemFriendly(Uri uri)
        {
            var localpath = uri.LocalPath;
            var rgx = new Regex(@"^(.*/[^/]*)\.([^/\.]*)$");
            var match = rgx.Match(localpath);
            string path;
            string extension;
            if (!match.Success)
            {
                path = localpath + "/index";
                extension = "html";
            }
            else
            {
                path = match.Groups[1].Value;
                extension = match.Groups[2].Value;
            }
            var query = uri.Query;
            if (query.Length > 0)
            {
                foreach (var c in Path.GetInvalidFileNameChars())
                {
                    query = query.Replace(c, '_');
                }
            }
            localpath = $"{path}{query}.{extension}";
            return new Uri(localpath.Substring(1), UriKind.Relative);
        }

        private Uri MakeUri(string url)
        {
            if (url.Contains(":"))
            {
                return new Uri(url);
            }
            var newUri = new Uri(url, UriKind.Relative);
            var absolute = newUri.IsAbsoluteUri ? newUri : new Uri(_uri, url);
            return absolute;
        }
    }

    public class ParseResult
    {
        public ParseResult()
        {
            Uris = new List<ParseItem>();
        }

        public string Payload { get; set; }

        public ICollection<ParseItem> Uris { get; set; }
    }

    public class ParseItem
    {
        public ParseItem(Uri siteUri, Uri fileUri)
        {
            SiteUri = siteUri;
            FileUri = fileUri;
        }

        public Uri FileUri { get; set; }

        public Uri SiteUri { get; set; }
    }
}
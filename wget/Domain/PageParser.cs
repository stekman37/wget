
using System.Collections.Generic;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace wget.Domain
{
    public class PageParser
    {
        private readonly string _urlFilter;
        private readonly Regex _javaScriptFilter;
        
        public PageParser(string urlFilter)
        {
            _urlFilter = urlFilter;
            _javaScriptFilter = new Regex(@"window.open\s?\(\s?""([^""]*)|location\.href\s?=\s?""([^""]*)",RegexOptions.IgnoreCase);
        }
       
        public IEnumerable<string> FindUrls(string page)
        {
            var htmlDoc = new HtmlDocument {OptionFixNestedTags = true};
            htmlDoc.LoadHtml(page);           
            if (htmlDoc.DocumentNode != null)
            {
                var htmlNodeCollection = htmlDoc.DocumentNode.SelectNodes("//*/@href");
                if (htmlNodeCollection != null)
                {
                    foreach (HtmlNode link in htmlNodeCollection)
                    {
                        var href = link.Attributes["href"].Value;
                        if (IsInSite(href))
                        {
                            yield return href;
                        }
                        else
                        {
                            if (href.StartsWith("javascript:"))
                            {
                                foreach (var p in GetScriptUris(href)) yield return p;
                            }
                        }
                    }
                    
                }
                var htmlNodeCollection2 = htmlDoc.DocumentNode.SelectNodes("//*/@src");
                if (htmlNodeCollection2 != null)
                {
                    foreach (var link in htmlNodeCollection2)
                    {
                        var src = link.Attributes["src"].Value;
                        if (IsInSite(src))
                        {
                            yield return src;
                        }
                    }
                }
                foreach (var script in htmlDoc.DocumentNode.SelectNodes("//script"))
                {
                    foreach (var p in GetScriptUris(script.InnerText)) yield return p;
                }
            }
        }

        private bool IsInSite(string href)
        {
            return !href.StartsWith("http") || href.StartsWith(_urlFilter);
        }

        private IEnumerable<string> GetScriptUris(string script)
        {
            Match match = _javaScriptFilter.Match(script);
            while (match.Success)
            {
                string href = match.Groups[1].Length>0?match.Groups[1].Value:match.Groups[2].Value;
                if (IsInSite(href))
                {
                    yield return href;
                }
                match = match.NextMatch();
            }
        }
    }
}
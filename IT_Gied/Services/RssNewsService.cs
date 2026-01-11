using System.ServiceModel.Syndication;
using System.Text.RegularExpressions;
using System.Xml;
using IT_Gied.Models;
using Microsoft.Extensions.Caching.Memory;

namespace IT_Gied.Services
{
    public class RssNewsService
    {
        private readonly HttpClient _http;
        private readonly IMemoryCache _cache;

        public RssNewsService(HttpClient http, IMemoryCache cache)
        {
            _http = http;
            _cache = cache;
        }

        // ✅ IT / Tech RSS sources (mix of news + dev + security + cloud + AI)
        // If any feed fails, it will be skipped without breaking the page.
        private static readonly (string Source, string Category, string FeedUrl)[] Feeds =
 {
    // Programming / Dev
    ("Stack Overflow Blog", "Programming", "https://stackoverflow.blog/feed/"),
    ("Microsoft Dev Blogs", "Programming", "https://devblogs.microsoft.com/feed/"),
    ("FreeCodeCamp", "Programming", "https://www.freecodecamp.org/news/rss/"),
    ("HackerNoon", "Programming", "https://hackernoon.com/feed"),

    // Courses / Learning
    ("Coursera Deals", "Courses", "https://blog.coursera.org/tag/deals/feed/"),
    ("Coursera New Courses", "Courses", "https://blog.coursera.org/category/new-courses/feed/"),
    ("Class Central", "Courses", "https://www.classcentral.com/report/feed/"),
    ("edX Blog", "Courses", "https://blog.edx.org/rss.xml"),
};




        public async Task<List<NewsCardVM>> GetLatestAsync(int perFeed = 4, int cacheMinutes = 10)
        {
            var cacheKey = $"dynamic-news-latest-{perFeed}";

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheMinutes);

                var all = new List<NewsCardVM>();

                foreach (var f in Feeds)
                {
                    try
                    {
                        all.AddRange(await ReadFeedAsync(f.Source, f.Category, f.FeedUrl, perFeed));
                    }
                    catch
                    {
                        // skip failing feeds
                    }
                }

                return all
                    .Where(x => !string.IsNullOrWhiteSpace(x.Title) && !string.IsNullOrWhiteSpace(x.Url))
                    .OrderByDescending(x => x.Published ?? DateTimeOffset.MinValue)
                    .ToList();

            }) ?? new List<NewsCardVM>();
        }

        private async Task<List<NewsCardVM>> ReadFeedAsync(string source, string category, string url, int take)
        {
            using var stream = await _http.GetStreamAsync(url);
            using var reader = XmlReader.Create(stream);
            var feed = SyndicationFeed.Load(reader);

            var items = feed?.Items?.Take(take) ?? Enumerable.Empty<SyndicationItem>();
            var result = new List<NewsCardVM>();

            foreach (var i in items)
            {
                var title = (i.Title?.Text ?? "").Trim();
                var link = i.Links.FirstOrDefault()?.Uri?.ToString() ?? "";

                var rawSummary = (i.Summary?.Text ?? "").Trim();
                var summary = CleanSummary(rawSummary);
                var imageUrl = TryGetImageUrl(i, rawSummary);

                var published = (i.PublishDate != DateTimeOffset.MinValue)
                    ? i.PublishDate
                    : (i.LastUpdatedTime != DateTimeOffset.MinValue ? i.LastUpdatedTime : (DateTimeOffset?)null);

                result.Add(new NewsCardVM
                {
                    Source = source,
                    Category = category,
                    Title = title,
                    Url = link,
                    Published = published,
                    Summary = string.IsNullOrWhiteSpace(summary) ? null : summary,
                    ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl
                });
            }

            return result;
        }

        private static string? TryGetImageUrl(SyndicationItem item, string rawSummaryHtml)
        {
            // enclosure
            var enclosure = item.Links.FirstOrDefault(l =>
                string.Equals(l.RelationshipType, "enclosure", StringComparison.OrdinalIgnoreCase));

            if (enclosure?.Uri != null)
            {
                var u = enclosure.Uri.ToString();
                if (LooksLikeImage(u)) return u;
            }

            // media:* extensions
            foreach (var ext in item.ElementExtensions)
            {
                try
                {
                    var name = (ext.OuterName ?? "").ToLowerInvariant();
                    if (name is "content" or "thumbnail" or "image")
                    {
                        using var r = ext.GetReader();
                        var xml = r.ReadOuterXml();

                        var url = ExtractXmlAttribute(xml, "url")
                                  ?? ExtractXmlAttribute(xml, "src")
                                  ?? ExtractXmlAttribute(xml, "href");

                        if (!string.IsNullOrWhiteSpace(url) && LooksLikeImage(url))
                            return url;
                    }
                }
                catch { }
            }

            // first <img src="..."> in summary HTML
            if (!string.IsNullOrWhiteSpace(rawSummaryHtml))
            {
                var img = ExtractFirstImgSrc(rawSummaryHtml);
                if (!string.IsNullOrWhiteSpace(img) && LooksLikeImage(img))
                    return img;
            }

            return null;
        }

        private static string CleanSummary(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return "";

            html = Regex.Replace(html, "<(script|style)[^>]*>.*?</\\1>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            var text = Regex.Replace(html, "<.*?>", " ").Trim();

            text = System.Net.WebUtility.HtmlDecode(text);
            text = Regex.Replace(text, "\\s+", " ").Trim();

            const int max = 160;
            if (text.Length > max) text = text.Substring(0, max).Trim() + "...";

            return text;
        }

        private static bool LooksLikeImage(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            url = url.ToLowerInvariant();
            return url.Contains(".jpg") || url.Contains(".jpeg") || url.Contains(".png") || url.Contains(".webp") || url.Contains(".gif");
        }

        private static string? ExtractFirstImgSrc(string html)
        {
            var m = Regex.Match(html, "<img[^>]+src=[\"']([^\"']+)[\"']", RegexOptions.IgnoreCase);
            return m.Success ? m.Groups[1].Value : null;
        }

        private static string? ExtractXmlAttribute(string xml, string attrName)
        {
            var pattern = attrName + "=\"";
            var idx = xml.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;

            idx += pattern.Length;
            var end = xml.IndexOf("\"", idx);
            if (end <= idx) return null;

            return xml.Substring(idx, end - idx);
        }
    }
}

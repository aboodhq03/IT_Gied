using System;

namespace IT_Gied.Models
{
    public class NewsCardVM
    {
        public string Source { get; set; } = "";    // Coursera, KDnuggets, MIT News
        public string Category { get; set; } = "";  // AI, Data, IT, E-Learning
        public string Title { get; set; } = "";
        public string Url { get; set; } = "";
        public DateTimeOffset? Published { get; set; }
        public string? Summary { get; set; }
        public string? ImageUrl { get; set; }
    }
}

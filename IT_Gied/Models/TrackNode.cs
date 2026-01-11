namespace IT_Gied.Models
{
    public class TrackNode
    {
        public int Id { get; set; }

        public int TrackId { get; set; }
        public Track Track { get; set; }

        public string Code { get; set; } = "";   // WD-01
        public string Name { get; set; } = "";   // HTML Basics

        // موقع العقدة في الصفحة (يدوي)
        public int X { get; set; }
        public int Y { get; set; }
        public string? DetailsUrl { get; set; }

    }

}

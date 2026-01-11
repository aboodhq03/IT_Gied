namespace IT_Gied.Models
{
    public class TrackNodeVM
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsUnlocked { get; set; }
        public string? DetailsUrl { get; set; }

    }

}

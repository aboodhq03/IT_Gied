namespace IT_Gied.Models
{
    public class UserTrackProgress
    {
        public int Id { get; set; }

        public string UserId { get; set; }
        public int TrackNodeId { get; set; }

        public bool IsCompleted { get; set; }
    }

}

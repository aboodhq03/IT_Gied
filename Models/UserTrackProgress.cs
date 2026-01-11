using System;
using System.ComponentModel.DataAnnotations;

namespace IT_Gied.Models
{
    public class UserTrackProgress
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = "";
        public ApplicationUser User { get; set; } = null!;

        public int TrackUnitId { get; set; }
        public TrackUnit TrackUnit { get; set; } = null!;

        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}

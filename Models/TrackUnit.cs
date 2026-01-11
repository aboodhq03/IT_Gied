using System.ComponentModel.DataAnnotations;

namespace IT_Gied.Models
{
    /// <summary>
    /// A single node/unit inside a Track (shown in the roadmap).
    /// Example: HTML Basics, CSS Basics, JavaScript Fundamentals.
    /// </summary>
    public class TrackUnit
    {
        public int Id { get; set; }

        public int TrackId { get; set; }
        public Track Track { get; set; } = null!;

        [Required, MaxLength(50)]
        public string Code { get; set; } = "";

        [Required, MaxLength(200)]
        public string Name { get; set; } = "";

        [MaxLength(600)]
        public string? Description { get; set; }

        /// <summary>
        /// A URL to open when clicking "details".
        /// </summary>
        [MaxLength(500)]
        public string? DetailsUrl { get; set; }

        /// <summary>
        /// Optional: order hint if you later want list view.
        /// </summary>
        public int Order { get; set; }
    }
}

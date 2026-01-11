using System.ComponentModel.DataAnnotations;

namespace IT_Gied.Models
{
    /// <summary>
    /// External learning track (e.g., WebDev, AI).
    /// </summary>
    public class Track
    {
        public int Id { get; set; }

        [Required, MaxLength(60)]
        public string Slug { get; set; } = ""; // e.g. "webdev"

        [Required, MaxLength(200)]
        public string Name { get; set; } = "";

        [MaxLength(600)]
        public string? Description { get; set; }

        [MaxLength(400)]
        public string? ImageUrl { get; set; }
    }
}

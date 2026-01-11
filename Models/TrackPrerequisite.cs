using System.ComponentModel.DataAnnotations;

namespace IT_Gied.Models
{
    public class TrackPrerequisite
    {
        public int Id { get; set; }

        public int TrackId { get; set; }
        public Track Track { get; set; } = null!;

        public int UnitId { get; set; }
        public TrackUnit Unit { get; set; } = null!;

        public int PrerequisiteUnitId { get; set; }
        public TrackUnit PrerequisiteUnit { get; set; } = null!;

        /// <summary>
        /// Keep it compatible with your existing roadmap logic.
        /// Values: "Prerequisite" or "Concurrent" (optional).
        /// </summary>
        [Required, MaxLength(20)]
        public string RelationType { get; set; } = "Prerequisite";
    }
}

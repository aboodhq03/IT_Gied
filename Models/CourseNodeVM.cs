using System.Collections.Generic;

namespace IT_Gied.Models
{
    public class CourseNodeVM
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;

        public string? Description { get; set; }

        public List<int> Unlocks { get; set; } = new List<int>();

        public bool IsCompleted { get; set; }
        public bool IsUnlocked { get; set; }
        public string? Grade { get; set; }
        public double AverageRating { get; set; }
        public double AverageDifficulty { get; set; }
    }
}

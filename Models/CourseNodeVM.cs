using System.Collections.Generic;

namespace IT_Gied.Models
{
    public class CourseNodeVM
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }

        public string? Description { get; set; }

        public List<int> Unlocks { get; set; } = new List<int>();

        public bool IsCompleted { get; set; }
        public bool IsUnlocked { get; set; }
    }
}

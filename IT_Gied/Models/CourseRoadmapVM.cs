namespace IT_Gied.Models
{
    public class CourseRoadmapVM
    {
        public int CourseId { get; set; }
        public string CourseName { get; set; }

        public bool IsCompleted { get; set; }
        public bool IsUnlocked { get; set; }
    }
}

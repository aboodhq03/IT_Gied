namespace IT_Gied.Models
{
    public class RecommendedCourseDto
    {
        public int CourseId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int CreditHours { get; set; }
        public string Status { get; set; } = string.Empty; // e.g., "Retake", "New"
        public string? Description { get; set; }
        public string? Prerequisites { get; set; }
        public int PriorityScore { get; set; }
        public double AverageDifficulty { get; set; }
    }
}

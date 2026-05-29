namespace IT_Gied.Models
{
    public class GraduationPlanViewModel
    {
        public string? Plan { get; set; }
        public double? GPA { get; set; }
        public int TargetSemesters { get; set; } = 4; // Default to 4 semesters
    }
}

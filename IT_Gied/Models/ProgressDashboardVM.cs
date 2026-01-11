namespace IT_Gied.Models
{
    public class ProgressDashboardVM
    {
        public string StudentName { get; set; } = "";
        public int CompletedHours { get; set; }

        public int TotalCourses { get; set; }
        public int CompletedCourses { get; set; }
        public int UnlockedCourses { get; set; }
        public int LockedCourses { get; set; }
        public double CompletionPercent { get; set; }
        public string? Image_Name { get; set; }

        public decimal? CumulativeGpa { get; set; }

    }
}

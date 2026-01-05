namespace IT_Gied.Models
{
    public class ProgressDashboardVM
    {
        public int TotalCourses { get; set; }
        public int CompletedCourses { get; set; }

      
        public int UnlockedCourses { get; set; }

        public int LockedCourses { get; set; }

      
        public double CompletionPercent { get; set; }
    }
}

using System.Collections.Generic;

namespace IT_Gied.Models
{
    public class RecommendationResult
    {
        public int UserRequestedCredits { get; set; }
        public int TotalRecommendedCredits { get; set; }
        public double UserGpa { get; set; }
        public List<RecommendedCourseDto> Courses { get; set; } = new();
        public string? AiExplanation { get; set; }
    }
}

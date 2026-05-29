using System.Collections.Generic;

namespace IT_Gied.Models
{
    public class AdvisorViewModel
    {
        public int AllowedCreditHours { get; set; }

        public double? GPA { get; set; }
        public string? CareerGoal { get; set; }

        public RecommendationResult? Result { get; set; }

        public string? AIExplanation { get; set; }
        
        public string? AdditionalNotes { get; set; }
    }
}
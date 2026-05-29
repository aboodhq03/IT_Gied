using System.Collections.Generic;

namespace IT_Gied.Models
{
    public class StudentBenefitsPageViewModel
    {
        public List<StudentBenefit> Benefits { get; set; } = new();
        public List<string> Categories { get; set; } = new();
        public string? SelectedCategory { get; set; }
    }
}

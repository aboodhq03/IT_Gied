using IT_Gied.Models;
using IT_Gied.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IT_Gied.Controllers
{
    public class StudentBenefitsController : Controller
    {
        private readonly IStudentBenefitService _studentBenefitService;
        private static readonly string[] BenefitCategories = new[]
        {
            "Developer Tools",
            "Student Discounts",
            "AI & Cloud Credits",
            "Learning Platforms",
            "Design Tools"
        };

        public StudentBenefitsController(IStudentBenefitService studentBenefitService)
        {
            _studentBenefitService = studentBenefitService;
        }

        public async Task<IActionResult> Index(string? category)
        {
            var benefits = await _studentBenefitService.GetActiveBenefitsAsync();

            if (!string.IsNullOrWhiteSpace(category))
            {
                benefits = benefits
                    .Where(x => x.Category.Equals(category, System.StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var model = new StudentBenefitsPageViewModel
            {
                Benefits = benefits,
                Categories = BenefitCategories.ToList(),
                SelectedCategory = category
            };

            return View(model);
        }
    }
}

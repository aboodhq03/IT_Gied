using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using IT_Gied.Models;
using IT_Gied.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IT_Gied.Controllers
{
    [Authorize]
    public class AdvisorController : Controller
    {
        private readonly AcademicAdvisorService _advisorService;
        private readonly GeminiService _geminiService;
        private readonly AiExplanationService _aiExplanationService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _context;

        public AdvisorController(
            AcademicAdvisorService advisorService,
            GeminiService geminiService,
            AiExplanationService aiExplanationService,
            UserManager<ApplicationUser> userManager,
            AppDbContext context)
        {
            _advisorService = advisorService;
            _geminiService = geminiService;
            _aiExplanationService = aiExplanationService;
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var gpa = await _advisorService.GetUserGpaAsync(userId);

            return View(new AdvisorViewModel { GPA = gpa, AllowedCreditHours = 15 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetRecommendation(AdvisorViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            double gpa = await _advisorService.GetUserGpaAsync(userId);
            model.GPA = gpa;

            var result = await _advisorService.MakeRecommendationAsync(
                userId,
                model.AllowedCreditHours,
                gpa,
                model.AdditionalNotes
            );


            model.Result = result;

            model.AIExplanation = result.AiExplanation;

            return View("Index", model);
        }

        [HttpGet]
        public async Task<IActionResult> Chat()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var history = await _context.AdvisorChatHistories
                .Where(h => h.UserId == userId)
                .OrderByDescending(h => h.CreatedAt)
                .Take(20)
                .OrderBy(h => h.CreatedAt)
                .ToListAsync();

            return View(new ChatViewModel { History = history });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Chat(ChatViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            string answer = "تعذر الحصول على إجابة في الوقت الحالي.";

            if (!string.IsNullOrWhiteSpace(model.Question))
            {
                double gpa = await _advisorService.GetUserGpaAsync(userId);

                var progresses = await _context.UserCourseProgresses
                    .Include(p => p.Course)
                    .Where(p => p.UserId == userId && p.Course != null)
                    .ToListAsync();

                var completedCourses = progresses
                    .Where(p => p.IsCompleted)
                    .Select(p => p.Course!)
                    .ToList();

                var failedCourses = progresses
                    .Where(p => !p.IsCompleted)
                    .Select(p => p.Course!)
                    .ToList();

                var difficultyStats = await _context.CourseRatings
                    .GroupBy(r => r.CourseId)
                    .Select(g => new { CourseId = g.Key, Avg = g.Average(r => (double)r.Difficulty) })
                    .ToDictionaryAsync(x => x.CourseId, x => x.Avg);

                var passedIds = completedCourses.Select(c => c.Id).ToHashSet();
                var allPrereqs = await _context.CoursePrerequisites.ToListAsync();

                var recommendedCourses = await _context.Courses
                    .Where(c => !passedIds.Contains(c.Id))
                    .ToListAsync();

                var availableRecommended = recommendedCourses
                    .Where(course =>
                    {
                        var prereqs = allPrereqs.Where(p => p.CourseId == course.Id).ToList();
                        return prereqs.All(p => passedIds.Contains(p.PrerequisiteCourseId));
                    })
                    .Select(course => new RecommendedCourseDto
                    {
                        CourseId = course.Id,
                        Code = course.Code,
                        Name = course.Name,
                        CreditHours = course.CreditHours,
                        Description = course.Description,
                        AverageDifficulty = difficultyStats.ContainsKey(course.Id)
                                            ? difficultyStats[course.Id] : 0,
                        Status = progresses.Any(p => !p.IsCompleted && p.CourseId == course.Id)
                                 ? "Retake" : "New"
                    })
                    .OrderByDescending(c => c.AverageDifficulty)
                    .Take(10)
                    .ToList();

                answer = await _aiExplanationService.AnswerStudentQuestion(
                    question: model.Question,
                    gpa: gpa,
                    completedCourses: completedCourses,
                    failedCourses: failedCourses,
                    recommendedCourses: availableRecommended,
                    careerGoal: null
                );

                _context.AdvisorChatHistories.Add(new AdvisorChatHistory
                {
                    UserId = userId,
                    Question = model.Question.Trim(),
                    Answer = answer,
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }

            var history = await _context.AdvisorChatHistories
                .Where(h => h.UserId == userId)
                .OrderByDescending(h => h.CreatedAt)
                .Take(20)
                .OrderBy(h => h.CreatedAt)
                .ToListAsync();

            return View(new ChatViewModel
            {
                History = history,
                Answer = answer,
                Question = string.Empty
            });
        }
    }
}
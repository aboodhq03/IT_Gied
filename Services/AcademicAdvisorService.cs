using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IT_Gied.Models;
using Microsoft.EntityFrameworkCore;

namespace IT_Gied.Services
{
    public class AcademicAdvisorService
    {
        private readonly AppDbContext _context;
        private readonly GeminiService _geminiService;

        public AcademicAdvisorService(AppDbContext context, GeminiService geminiService)
        {
            _context = context;
            _geminiService = geminiService;
        }

        public async Task<RecommendationResult> MakeRecommendationAsync(
            string userId,
            int requestedCredits,
            double? providedGpa = null,
            string? additionalNotes = null)
        {
            double gpa = providedGpa ?? await GetUserGpaAsync(userId);

            var completedProgresses = await _context.UserCourseProgresses
                .Include(p => p.Course)
                .Where(p => p.UserId == userId && p.IsCompleted && p.Course != null)
                .ToListAsync();

            var passedCourseIds = completedProgresses.Select(p => p.CourseId).ToHashSet();

            var failedProgresses = await _context.UserCourseProgresses
                .Include(p => p.Course)
                .Where(p => p.UserId == userId && !p.IsCompleted && p.Course != null)
                .ToListAsync();

            var failedCourseIds = failedProgresses.Select(p => p.CourseId).ToHashSet();

            var difficultyStats = await _context.CourseRatings
                .GroupBy(r => r.CourseId)
                .Select(g => new { CourseId = g.Key, Avg = g.Average(r => (double)r.Difficulty) })
                .ToDictionaryAsync(x => x.CourseId, x => x.Avg);

            var allCourses = await _context.Courses.ToListAsync();
            var allPrereqs = await _context.CoursePrerequisites.ToListAsync();

            var availableForAi = new List<AiCourseInput>();

            foreach (var course in allCourses)
            {
                if (passedCourseIds.Contains(course.Id)) continue;

                var prereqs = allPrereqs.Where(p => p.CourseId == course.Id).ToList();
                bool prereqsMet = prereqs.All(p => passedCourseIds.Contains(p.PrerequisiteCourseId));

                if (!prereqsMet) continue;

                availableForAi.Add(new AiCourseInput
                {
                    CourseId = course.Id,
                    Name = course.Name,
                    Code = course.Code,
                    CreditHours = course.CreditHours,
                    Description = course.Description,
                    AverageDifficulty = difficultyStats.ContainsKey(course.Id) ? difficultyStats[course.Id] : 0,
                    Status = failedCourseIds.Contains(course.Id) ? "Retake" : "New"
                });
            }

            var completedForAi = completedProgresses
                .Where(p => p.Course != null)
                .Select(p => new AiCompletedCourseInput
                {
                    Name = p.Course!.Name,
                    Code = p.Course.Code,
                    Grade = p.Grade
                }).ToList();

            var aiResult = await _geminiService.GenerateAiScheduleAsync(
                gpa,
                requestedCredits,
                availableForAi,
                completedForAi,
                additionalNotes
            );

            // DEBUG مؤقت
            Console.WriteLine($"=== AI DEBUG ===");
            Console.WriteLine($"Available: {availableForAi.Count} courses");
            Console.WriteLine($"Selected IDs: [{string.Join(",", aiResult.SelectedCourseIds)}]");
            Console.WriteLine($"TotalCredits: {aiResult.TotalCredits}");
            Console.WriteLine($"Explanation: {aiResult.Explanation}");
            Console.WriteLine($"================");

            var selectedIds = aiResult.SelectedCourseIds.ToHashSet();
            var selectedCourses = availableForAi
                .Where(c => selectedIds.Contains(c.CourseId))
                .Select(c => new RecommendedCourseDto
                {
                    CourseId = c.CourseId,
                    Name = c.Name,
                    Code = c.Code,
                    CreditHours = c.CreditHours,
                    Description = c.Description,
                    AverageDifficulty = c.AverageDifficulty,
                    Status = c.Status
                }).ToList();

            return new RecommendationResult
            {
                UserRequestedCredits = requestedCredits,
                UserGpa = gpa,
                TotalRecommendedCredits = selectedCourses.Sum(c => c.CreditHours),
                Courses = selectedCourses,
                AiExplanation = aiResult.Explanation
            };
        }

        public async Task<double> GetUserGpaAsync(string userId)
        {
            var calculatedGpa = await CalculateGpaFromProgressAsync(userId);
            if (calculatedGpa.HasValue) return calculatedGpa.Value;

            var gpaRecord = await _context.UserGpas.AsNoTracking()
                .FirstOrDefaultAsync(g => g.UserId == userId);
            return gpaRecord != null ? (double)gpaRecord.CumulativeGpa : 0.0;
        }

        public async Task<double> UpdateUserGpaAsync(string userId)
        {
            var calculatedGpa = await CalculateGpaFromProgressAsync(userId);
            if (!calculatedGpa.HasValue) return 0;

            var gpaRecord = await _context.UserGpas.FirstOrDefaultAsync(g => g.UserId == userId);
            if (gpaRecord == null)
            {
                gpaRecord = new UserGpa { UserId = userId };
                _context.UserGpas.Add(gpaRecord);
            }

            gpaRecord.CumulativeGpa = (decimal)calculatedGpa.Value;
            gpaRecord.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return calculatedGpa.Value;
        }

        private async Task<double?> CalculateGpaFromProgressAsync(string userId)
        {
            var progress = await _context.UserCourseProgresses
                .Include(ucp => ucp.Course)
                .Where(ucp => ucp.UserId == userId && ucp.IsCompleted && ucp.Grade != null)
                .ToListAsync();

            if (!progress.Any()) return null;

            double totalPoints = 0;
            int totalHours = 0;

            foreach (var p in progress)
            {
                if (p.Course != null)
                {
                    var points = UserCourseProgress.LetterToPoint(p.Grade);
                    if (points.HasValue)
                    {
                        totalPoints += points.Value * p.Course.CreditHours;
                        totalHours += p.Course.CreditHours;
                    }
                }
            }

            if (totalHours == 0) return null;

            double rawGpa = totalPoints / totalHours;
            if (rawGpa > 4.0) return Math.Round(rawGpa / 25.0, 2);
            return Math.Round(rawGpa, 2);
        }

        public async Task<string> GetStudentTalentPromptAsync(string userId, RecommendationResult result)
        {
            var history = await _context.UserCourseProgresses
                .Include(ucp => ucp.Course)
                .Where(ucp => ucp.UserId == userId && ucp.IsCompleted && ucp.Grade != null)
                .ToListAsync();

            var gradesSummary = history.Where(x => x.Course != null)
                .Select(h => $"- {h.Course!.Name} ({h.Course.Code}): علامة {h.Grade}");

            var recommendedSummary = result.Courses.Select(r =>
                $"- {r.Name} ({r.Code}) | صعوبة: {r.AverageDifficulty:0.0}/10 | الحالة: {r.Status}");

            return $@"بيانات الطالب:
المعدل التراكمي: {result.UserGpa:0.00}

سجل العلامات السابقة:
{string.Join("\n", gradesSummary)}

الجدول المقترح من الذكاء الاصطناعي:
{string.Join("\n", recommendedSummary)}

المهمة:
أنت مرشد أكاديمي. اشرح للطالب بالعربي بأسلوب دافئ ومحفز:
1. لماذا هذه المواد مناسبة لمستواه بناءً على صعوبتها ومعدله.
2. كيف يتعامل مع المواد الأصعب فيها.
3. إذا كان معدله منخفضاً، طمئنه وشجعه.";
        }
    }
}
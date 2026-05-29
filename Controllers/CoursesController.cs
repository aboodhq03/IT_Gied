using IT_Gied.Models;
using IT_Gied.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace IT_Gied.Controllers
{
    [Authorize]
    public class CoursesController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AcademicAdvisorService _advisorService;

        public CoursesController(AppDbContext db, UserManager<ApplicationUser> userManager, AcademicAdvisorService advisorService)
        {
            _db = db;
            _userManager = userManager;
            _advisorService = advisorService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var userProgress = _db.UserCourseProgresses
                .Where(x => x.UserId == userId)
                .ToDictionary(x => x.CourseId);

            var ratingStats = _db.CourseRatings
                .GroupBy(r => r.CourseId)
                .Select(g => new { 
                    CourseId = g.Key, 
                    AvgScore = g.Average(r => r.Score),
                    AvgDifficulty = g.Average(r => r.Difficulty)
                })
                .ToDictionary(x => x.CourseId);

            var courses = _db.Courses.ToList();
            var prereqLinks = _db.CoursePrerequisites.ToList();

            var nodes = courses.Select(c =>
            {
                var prereqs = prereqLinks
                    .Where(p => p.CourseId == c.Id && p.RelationType == "Prerequisite")
                    .Select(p => p.PrerequisiteCourseId)
                    .ToList();

                bool isCompleted = userProgress.ContainsKey(c.Id) && userProgress[c.Id].IsCompleted;
                bool isUnlocked = prereqs.Count == 0 || prereqs.All(p => userProgress.ContainsKey(p) && userProgress[p].IsCompleted);

                return new CourseNodeVM
                {
                    Id = c.Id,
                    Name = c.Name,
                    Code = c.Code,
                    Description = c.Description,
                    IsCompleted = isCompleted,
                    IsUnlocked = isUnlocked,
                    Grade = userProgress.ContainsKey(c.Id) ? userProgress[c.Id].Grade : null,
                    AverageRating = ratingStats.ContainsKey(c.Id) ? ratingStats[c.Id].AvgScore : 0,
                    AverageDifficulty = ratingStats.ContainsKey(c.Id) ? ratingStats[c.Id].AvgDifficulty : 0,
                    Unlocks = prereqLinks
                        .Where(p => p.PrerequisiteCourseId == c.Id && p.RelationType == "Prerequisite")
                        .Select(p => p.CourseId)
                        .ToList()
                };
            }).ToList();

            return View(nodes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleComplete(int courseId, string? grade)
        {
            await Toggle(courseId, grade);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCompleteAjax(int courseId, string? grade)
        {
            try
            {
                var isCompleted = await Toggle(courseId, grade);
                return Json(new { ok = true, isCompleted });
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                return Json(new { ok = false, message = msg });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateGradeAjax(int courseId, string grade)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var row = _db.UserCourseProgresses
                    .FirstOrDefault(x => x.UserId == userId && x.CourseId == courseId);

                if (row != null && row.IsCompleted)
                {
                    row.Grade = grade;
                    _db.SaveChanges();
                    
                    await _advisorService.UpdateUserGpaAsync(userId);
                    
                    return Json(new { ok = true });
                }
                return Json(new { ok = false, message = "Course not completed or not found." });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddRating(int courseId, int score, int difficulty, string? comment)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            if (score < 1 || score > 10) return BadRequest("Rating must be between 1 and 10.");
            if (difficulty < 1 || difficulty > 10) return BadRequest("Difficulty must be between 1 and 10.");

            var rating = await _db.CourseRatings
                .FirstOrDefaultAsync(r => r.UserId == userId && r.CourseId == courseId);

            if (rating == null)
            {
                rating = new CourseRating
                {
                    UserId = userId,
                    CourseId = courseId,
                    Score = score,
                    Difficulty = difficulty,
                    Comment = comment,
                    CreatedAt = DateTime.UtcNow
                };
                _db.CourseRatings.Add(rating);
            }
            else
            {
                rating.Score = score;
                rating.Difficulty = difficulty;
                rating.Comment = comment;
                rating.CreatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Details(int id)
        {
            var course = _db.Courses.FirstOrDefault(c => c.Id == id);
            if (course == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(course.DetailsUrl))
                return Redirect(course.DetailsUrl);

            if (!string.IsNullOrWhiteSpace(course.EDetailsUrl))
                return Redirect(course.EDetailsUrl);
            
            return Content("No details link available for this course.");
        }

        private async Task<bool> Toggle(int courseId, string? grade = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                throw new Exception("User ID not found. Please log in again.");

            var row = _db.UserCourseProgresses
                .FirstOrDefault(x => x.UserId == userId && x.CourseId == courseId);

            if (row == null)
            {
                row = new UserCourseProgress
                {
                    UserId = userId,
                    CourseId = courseId,
                    IsCompleted = true,
                    CompletedAt = DateTime.UtcNow,
                    Grade = grade
                };
                _db.UserCourseProgresses.Add(row);
            }
            else
            {
                row.IsCompleted = !row.IsCompleted;
                row.CompletedAt = row.IsCompleted ? DateTime.UtcNow : (DateTime?)null;
                if (row.IsCompleted && grade != null) row.Grade = grade;
            }

            _db.SaveChanges();

            await _advisorService.UpdateUserGpaAsync(userId);

            return row.IsCompleted;
        }

        [HttpGet]
        public IActionResult Links()
        {
            var links = _db.CoursePrerequisites
                .Select(x => new { from = x.PrerequisiteCourseId, to = x.CourseId, type = x.RelationType })
                .ToList();

            return Json(links);
        }
    }
}

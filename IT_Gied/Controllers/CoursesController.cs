using IT_Gied.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;

namespace IT_Gied.Controllers
{
    [Authorize]
    public class CoursesController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public CoursesController(AppDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var completedCourseIds = _db.UserCourseProgresses
                .Where(x => x.UserId == userId && x.IsCompleted)
                .Select(x => x.CourseId)
                .ToHashSet();

            var courses = _db.Courses.ToList();
            var prereqLinks = _db.CoursePrerequisites.ToList();

            var nodes = courses.Select(c =>
            {
                var prereqs = prereqLinks
                    .Where(p => p.CourseId == c.Id && p.RelationType == "Prerequisite")
                    .Select(p => p.PrerequisiteCourseId)
                    .ToList();

                bool isUnlocked = prereqs.Count == 0 || prereqs.All(p => completedCourseIds.Contains(p));

                return new CourseNodeVM
                {
                    Id = c.Id,
                    Name = c.Name,
                    Code = c.Code,
                    Description = c.Description,
                    IsCompleted = completedCourseIds.Contains(c.Id),
                    IsUnlocked = isUnlocked,

                    Unlocks = prereqLinks
                        .Where(p => p.PrerequisiteCourseId == c.Id && p.RelationType == "Prerequisite")
                        .Select(p => p.CourseId)
                        .ToList()
                };
            }).ToList();

            return View(nodes);
        }

        // هذا خلّيه fallback لو JS مش شغّال
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleComplete(int courseId)
        {
            Toggle(courseId);
            return RedirectToAction(nameof(Index));
        }

        // ===== روابط الخطوط (Prerequisite + Concurrent) =====
        [HttpGet]
        public IActionResult Links()
        {
            var links = _db.CoursePrerequisites
                .Where(x => x.PrerequisiteCourseId != null && x.CourseId != null) // احتياط
                .Select(x => new
                {
                    from = x.PrerequisiteCourseId,
                    to = x.CourseId,
                    type = x.RelationType
                })
                .ToList();

            return Json(links);
        }

        // ===== AJAX Toggle (بدون reload) =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleCompleteAjax(int courseId)
        {
            var isCompleted = Toggle(courseId);
            return Json(new { ok = true, isCompleted });
        }

        // ===== Helper: نفس منطق التبديل بمكان واحد =====
        private bool Toggle(int courseId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var row = _db.UserCourseProgresses
                .FirstOrDefault(x => x.UserId == userId && x.CourseId == courseId);

            if (row == null)
            {
                row = new UserCourseProgress
                {
                    UserId = userId,
                    CourseId = courseId,
                    IsCompleted = true,
                    CompletedAt = DateTime.UtcNow
                };
                _db.UserCourseProgresses.Add(row);
            }
            else
            {
                row.IsCompleted = !row.IsCompleted;
                row.CompletedAt = row.IsCompleted ? DateTime.UtcNow : (DateTime?)null;
            }

            _db.SaveChanges();
            return row.IsCompleted;
        }
        [HttpGet]
        public IActionResult Details(int id)
        {
            var course = _db.Courses.FirstOrDefault(c => c.Id == id);
            if (course == null) return NotFound();

            // 1) داخلي داخل المشروع (صفحة html في wwwroot)
            if (!string.IsNullOrWhiteSpace(course.DetailsUrl))
            {
                // إذا كانت /Html/... خلّيها Redirect مباشر
                // ملاحظة: هذا يتوقع أنها موجودة في wwwroot
                return Redirect(course.DetailsUrl);
            }

            // 2) خارجي
            if (!string.IsNullOrWhiteSpace(course.EDetailsUrl))
            {
                return Redirect(course.EDetailsUrl);
            }

            // 3) ما في رابط
            return Content("No details link for this course yet.");
        }


    }
}

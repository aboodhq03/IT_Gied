using System.Diagnostics;
using System.Security.Claims;
using IT_Gied.Models;
using IT_Gied.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IT_Gied.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RssNewsService _news;

        public HomeController(
            ILogger<HomeController> logger,
            AppDbContext db,
            UserManager<ApplicationUser> userManager,
            RssNewsService news)
        {
            _logger = logger;
            _db = db;
            _userManager = userManager;
            _news = news;
        }

        public async Task<IActionResult> Index()
        {
            // ✅ Guest ONLY: load news and return null model
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                ViewBag.TechNews = await _news.GetLatestAsync(perFeed: 4, cacheMinutes: 10);
                return View(null);
            }

            // ✅ Logged-in: do NOT load news (and avoid ViewBag null issues)
            ViewBag.TechNews = new List<NewsCardVM>();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var user = await _userManager.FindByIdAsync(userId);
            var email = user?.Email ?? User.Identity?.Name ?? "Student";
            var studentName = email.Contains("@") ? email.Split('@')[0] : email;

            var completedCourseIds = await _db.UserCourseProgresses
                .Where(x => x.UserId == userId && x.IsCompleted)
                .Select(x => x.CourseId)
                .ToListAsync();

            var totalCourses = await _db.Courses.CountAsync();
            var completedCourses = completedCourseIds.Count;

            var completedHours = await _db.Courses
                .Where(c => completedCourseIds.Contains(c.Id))
                .SumAsync(c => (int?)c.CreditHours) ?? 0;

            var completionPercent = totalCourses == 0
                ? 0
                : (completedCourses * 100.0) / totalCourses;

            var gpaRow = await _db.UserGpas.AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId);

            var vm = new ProgressDashboardVM
            {
                StudentName = studentName,
                TotalCourses = totalCourses,
                CompletedCourses = completedCourses,
                CompletedHours = completedHours,
                CompletionPercent = Math.Round(completionPercent, 1),
                Image_Name = user?.Image_Name,
                CumulativeGpa = gpaRow?.CumulativeGpa
            };

            return View(vm);
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }

        public IActionResult aboutus() => View();
    }
}

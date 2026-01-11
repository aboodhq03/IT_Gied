using System.Security.Claims;
using IT_Gied.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IT_Gied.Controllers
{
    [Authorize]
    public class GpaController : Controller
    {
        private readonly AppDbContext _db;

        public GpaController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var saved = await _db.UserGpas.AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId);

            return View(saved);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(decimal cumulativeGpa)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var row = await _db.UserGpas.FirstOrDefaultAsync(x => x.UserId == userId);

            if (row == null)
            {
                row = new UserGpa { UserId = userId };
                _db.UserGpas.Add(row);
            }

            row.CumulativeGpa = decimal.Round(cumulativeGpa, 2);
            row.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            TempData["Msg"] = "Cumulative GPA saved successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}

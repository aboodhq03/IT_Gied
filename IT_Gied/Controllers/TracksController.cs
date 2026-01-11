using IT_Gied.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IT_Gied.Controllers
{
    public class TracksController : Controller
    {
        private readonly AppDbContext _db;

        public TracksController(AppDbContext db)
        {
            _db = db;
        }

        [AllowAnonymous]
        public IActionResult Index() => View();

        [Authorize]
        public IActionResult WebDev()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var completedIds = _db.UserTrackProgresses
                .Where(p => p.UserId == userId && p.IsCompleted)
                .Select(p => p.TrackNodeId)
                .ToHashSet();

            var links = _db.TrackLinks.ToList();

            var nodes = _db.TrackNodes
                .Include(n => n.Track)
                .Where(n => n.Track.Slug == "webdev")
                .ToList()
                .Select(n =>
                {
                    var prereqs = links
                        .Where(l => l.ToNodeId == n.Id)
                        .Select(l => l.FromNodeId)
                        .ToList();

                    bool isCompleted = completedIds.Contains(n.Id);
                    bool isUnlocked = prereqs.Count == 0 || prereqs.All(pid => completedIds.Contains(pid));

                    return new TrackNodeVM
                    {
                        Id = n.Id,
                        Code = n.Code,
                        Name = n.Name,
                        X = n.X,
                        Y = n.Y,
                        IsCompleted = isCompleted,
                        IsUnlocked = isUnlocked,
                        DetailsUrl = n.DetailsUrl
                    };
                })
                .ToList();

            ViewBag.Links = links;
            return View(nodes);
        }

        // ✅ بدون JSON: نستقبل id مباشرة
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleComplete(int id)
        {
            if (id <= 0)
                return BadRequest("Invalid id");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var progress = await _db.UserTrackProgresses
                .FirstOrDefaultAsync(p => p.UserId == userId && p.TrackNodeId == id);

            if (progress == null)
            {
                _db.UserTrackProgresses.Add(new UserTrackProgress
                {
                    UserId = userId,
                    TrackNodeId = id,
                    IsCompleted = true
                });
            }
            else
            {
                progress.IsCompleted = !progress.IsCompleted;
            }

            await _db.SaveChangesAsync();

            // إذا زرّك يعمل fetch: OK
            return Ok();

            // إذا زرّك فورم عادي وبدك يرجع لنفس الصفحة:
            // return RedirectToAction(nameof(WebDev));
        }
    }
}

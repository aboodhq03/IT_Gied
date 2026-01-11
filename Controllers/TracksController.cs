using IT_Gied.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
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

        /// <summary>
        /// Public page for everyone: shows external tracks.
        /// </summary>
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Index()
        {
            // For now, show tracks from DB (seeded). Later you can add AI, etc.
            var tracks = _db.Tracks
                .OrderBy(x => x.Id)
                .ToList();

            return View(tracks);
        }

        /// <summary>
        /// WebDev roadmap page (private per student).
        /// </summary>
        [Authorize]
        [HttpGet]
        public IActionResult WebDev()
        {
            return TrackRoadmap("webdev");
        }

        /// <summary>
        /// Roadmap view builder (reuses the same style you used for Courses).
        /// </summary>
        private IActionResult TrackRoadmap(string slug)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var track = _db.Tracks.FirstOrDefault(t => t.Slug == slug);
            if (track == null) return NotFound();

            var completedUnitIds = _db.UserTrackProgresses
                .Where(x => x.UserId == userId && x.IsCompleted)
                .Select(x => x.TrackUnitId)
                .ToHashSet();

            var units = _db.TrackUnits.Where(x => x.TrackId == track.Id).ToList();
            var prereqLinks = _db.TrackPrerequisites.Where(x => x.TrackId == track.Id).ToList();

            var nodes = units.Select(u =>
            {
                var prereqs = prereqLinks
                    .Where(p => p.UnitId == u.Id && p.RelationType == "Prerequisite")
                    .Select(p => p.PrerequisiteUnitId)
                    .ToList();

                var isUnlocked = prereqs.Count == 0 || prereqs.All(p => completedUnitIds.Contains(p));

                return new TrackNodeVM
                {
                    Id = u.Id,
                    Name = u.Name,
                    Code = u.Code,
                    Description = u.Description,
                    IsCompleted = completedUnitIds.Contains(u.Id),
                    IsUnlocked = isUnlocked,
                    Unlocks = prereqLinks
                        .Where(p => p.PrerequisiteUnitId == u.Id && p.RelationType == "Prerequisite")
                        .Select(p => p.UnitId)
                        .ToList()
                };
            }).ToList();

            ViewBag.TrackSlug = track.Slug;
            ViewBag.TrackName = track.Name;
            return View("Roadmap", nodes);
        }

        /// <summary>
        /// Links used by the roadmap JS to draw edges.
        /// </summary>
        [Authorize]
        [HttpGet]
        public IActionResult Links(string slug)
        {
            var track = _db.Tracks.FirstOrDefault(t => t.Slug == slug);
            if (track == null) return NotFound();

            var links = _db.TrackPrerequisites
                .Where(x => x.TrackId == track.Id)
                .Select(x => new
                {
                    from = x.PrerequisiteUnitId,
                    to = x.UnitId,
                    type = x.RelationType
                })
                .ToList();

            return Json(links);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleComplete(int unitId, string slug)
        {
            Toggle(unitId);
            return RedirectToAction(nameof(WebDev));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleCompleteAjax(int unitId)
        {
            var isCompleted = Toggle(unitId);
            return Json(new { ok = true, isCompleted });
        }

        private bool Toggle(int unitId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var row = _db.UserTrackProgresses
                .FirstOrDefault(x => x.UserId == userId && x.TrackUnitId == unitId);

            if (row == null)
            {
                row = new UserTrackProgress
                {
                    UserId = userId,
                    TrackUnitId = unitId,
                    IsCompleted = true,
                    CompletedAt = DateTime.UtcNow
                };
                _db.UserTrackProgresses.Add(row);
            }
            else
            {
                row.IsCompleted = !row.IsCompleted;
                row.CompletedAt = row.IsCompleted ? DateTime.UtcNow : (DateTime?)null;
            }

            _db.SaveChanges();
            return row.IsCompleted;
        }

        /// <summary>
        /// Optional: open external details url if you set it for a unit.
        /// </summary>
        [Authorize]
        [HttpGet]
        public IActionResult UnitDetails(int id)
        {
            var unit = _db.TrackUnits.FirstOrDefault(x => x.Id == id);
            if (unit == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(unit.DetailsUrl))
                return Redirect(unit.DetailsUrl);

            return Content("No details link for this unit yet.");
        }
    }
}

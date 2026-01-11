using System;
using System.Collections.Generic;
using System.Linq;

namespace IT_Gied.Models
{
    /// <summary>
    /// Seeds a default WebDev roadmap (nodes + prerequisites) if the DB is empty.
    /// You can edit the node list at any time.
    /// </summary>
    public static class TrackSeed
    {
        public static void EnsureSeeded(AppDbContext db)
        {
            // If tracks already exist, do nothing.
            if (db.Tracks.Any()) return;

            var web = new Track
            {
                Slug = "webdev",
                Name = "Web Development",
                Description = "مسار تطوير الويب (خارجي) بخطة شجرية مثل خريطة الكورسات.",
                ImageUrl = "/img/tracks/web.jpg"
            };
            db.Tracks.Add(web);
            db.SaveChanges();

            // Create units (nodes)
            var units = new List<TrackUnit>
            {
                new TrackUnit { TrackId = web.Id, Code = "W1", Name = "HTML Basics", Description = "العناصر، الروابط، النماذج", Order = 1 },
                new TrackUnit { TrackId = web.Id, Code = "W2", Name = "CSS Basics", Description = "Selectors، Flexbox، Grid", Order = 2 },
                new TrackUnit { TrackId = web.Id, Code = "W3", Name = "Responsive Design", Description = "Media Queries، Mobile-first", Order = 3 },
                new TrackUnit { TrackId = web.Id, Code = "W4", Name = "JavaScript Fundamentals", Description = "DOM، Events، ES6", Order = 4 },
                new TrackUnit { TrackId = web.Id, Code = "W5", Name = "Git & GitHub", Description = "Version control، Branches", Order = 5 },
                new TrackUnit { TrackId = web.Id, Code = "W6", Name = "HTTP & APIs", Description = "REST، Fetch/Axios", Order = 6 },
                new TrackUnit { TrackId = web.Id, Code = "W7", Name = "Backend Basics", Description = "ASP.NET Core MVC basics", Order = 7 },
                new TrackUnit { TrackId = web.Id, Code = "W8", Name = "Database Basics", Description = "SQL + EF Core", Order = 8 },
                new TrackUnit { TrackId = web.Id, Code = "W9", Name = "Authentication", Description = "Identity، Cookies", Order = 9 },
                new TrackUnit { TrackId = web.Id, Code = "W10", Name = "Deploy", Description = "Hosting + environment", Order = 10 },
            };

            db.TrackUnits.AddRange(units);
            db.SaveChanges();

            int IdOf(string code) => db.TrackUnits.First(x => x.TrackId == web.Id && x.Code == code).Id;

            // Prerequisites (edges): FROM prereq -> TO unit
            var prereqs = new List<TrackPrerequisite>
            {
                // CSS after HTML
                new TrackPrerequisite { TrackId = web.Id, PrerequisiteUnitId = IdOf("W1"), UnitId = IdOf("W2"), RelationType = "Prerequisite" },
                // Responsive after CSS
                new TrackPrerequisite { TrackId = web.Id, PrerequisiteUnitId = IdOf("W2"), UnitId = IdOf("W3"), RelationType = "Prerequisite" },
                // JS after HTML + CSS (keep it simple: require CSS)
                new TrackPrerequisite { TrackId = web.Id, PrerequisiteUnitId = IdOf("W2"), UnitId = IdOf("W4"), RelationType = "Prerequisite" },
                // HTTP/APIs after JS
                new TrackPrerequisite { TrackId = web.Id, PrerequisiteUnitId = IdOf("W4"), UnitId = IdOf("W6"), RelationType = "Prerequisite" },
                // Backend after JS
                new TrackPrerequisite { TrackId = web.Id, PrerequisiteUnitId = IdOf("W4"), UnitId = IdOf("W7"), RelationType = "Prerequisite" },
                // DB after Backend
                new TrackPrerequisite { TrackId = web.Id, PrerequisiteUnitId = IdOf("W7"), UnitId = IdOf("W8"), RelationType = "Prerequisite" },
                // Auth after Backend + DB (require DB)
                new TrackPrerequisite { TrackId = web.Id, PrerequisiteUnitId = IdOf("W8"), UnitId = IdOf("W9"), RelationType = "Prerequisite" },
                // Deploy after Auth
                new TrackPrerequisite { TrackId = web.Id, PrerequisiteUnitId = IdOf("W9"), UnitId = IdOf("W10"), RelationType = "Prerequisite" },
            };

            db.TrackPrerequisites.AddRange(prereqs);
            db.SaveChanges();
        }
    }
}

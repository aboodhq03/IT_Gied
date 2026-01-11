using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IT_Gied.Models
{
    public static class ExternalTracksSeeder
    {
        /// <summary>
        /// Seeds a starter WebDev roadmap (Track + Units + Prerequisites) if it doesn't exist.
        /// Safe to run multiple times.
        /// </summary>
        public static void SeedWebDev(AppDbContext db)
        {
            if (db.Tracks.Any(t => t.Slug == "webdev"))
                return;

            var track = new Track
            {
                Slug = "webdev",
                Name = "Web Development",
                Description = "Front-End / Back-End fundamentals with a step-by-step roadmap.",
                ImageUrl = "/img/tracks/web.jpg"
            };
            db.Tracks.Add(track);
            db.SaveChanges();

            // Units (nodes)
            var units = new List<TrackUnit>
            {
                new TrackUnit{ TrackId = track.Id, Code="WD-01", Name="Internet & Tools", Description="How the web works, browser/devtools, VS Code, Git basics", Order=1 },
                new TrackUnit{ TrackId = track.Id, Code="WD-02", Name="HTML Fundamentals", Description="Semantic HTML, forms, accessibility basics", Order=2 },
                new TrackUnit{ TrackId = track.Id, Code="WD-03", Name="CSS Fundamentals", Description="Selectors, box model, layout, responsive", Order=3 },
                new TrackUnit{ TrackId = track.Id, Code="WD-04", Name="CSS Layout (Flex/Grid)", Description="Flexbox + CSS Grid deep dive", Order=4 },
                new TrackUnit{ TrackId = track.Id, Code="WD-05", Name="JavaScript Fundamentals", Description="Syntax, DOM, events, functions", Order=5 },
                new TrackUnit{ TrackId = track.Id, Code="WD-06", Name="JavaScript (Async)", Description="Promises, async/await, fetch APIs", Order=6 },
                new TrackUnit{ TrackId = track.Id, Code="WD-07", Name="HTTP & REST", Description="HTTP, APIs, JSON, Postman basics", Order=7 },
                new TrackUnit{ TrackId = track.Id, Code="WD-08", Name="Databases & SQL", Description="Relational basics, SQL CRUD", Order=8 },
                new TrackUnit{ TrackId = track.Id, Code="WD-09", Name="Back-End Basics", Description="Server-side concepts, MVC, auth", Order=9 },
                new TrackUnit{ TrackId = track.Id, Code="WD-10", Name="Deploy & Hosting", Description="Deploy basics, domain, SSL, CI/CD intro", Order=10 },
                new TrackUnit{ TrackId = track.Id, Code="WD-11", Name="Capstone Project", Description="Build a full web app end-to-end", Order=11 },
            };

            db.TrackUnits.AddRange(units);
            db.SaveChanges();

            // Map by code for prerequisites
            var byCode = db.TrackUnits
                .Where(x => x.TrackId == track.Id)
                .AsNoTracking()
                .ToDictionary(x => x.Code, x => x.Id);

            // Prerequisites (edges)
            var prereqs = new List<TrackPrerequisite>
            {
                // HTML after tools
                new TrackPrerequisite{ TrackId=track.Id, UnitId=byCode["WD-02"], PrerequisiteUnitId=byCode["WD-01"], RelationType="Prerequisite" },

                // CSS after HTML
                new TrackPrerequisite{ TrackId=track.Id, UnitId=byCode["WD-03"], PrerequisiteUnitId=byCode["WD-02"], RelationType="Prerequisite" },
                new TrackPrerequisite{ TrackId=track.Id, UnitId=byCode["WD-04"], PrerequisiteUnitId=byCode["WD-03"], RelationType="Prerequisite" },

                // JS after HTML+CSS
                new TrackPrerequisite{ TrackId=track.Id, UnitId=byCode["WD-05"], PrerequisiteUnitId=byCode["WD-02"], RelationType="Prerequisite" },
                new TrackPrerequisite{ TrackId=track.Id, UnitId=byCode["WD-05"], PrerequisiteUnitId=byCode["WD-03"], RelationType="Prerequisite" },

                // Async after JS fundamentals
                new TrackPrerequisite{ TrackId=track.Id, UnitId=byCode["WD-06"], PrerequisiteUnitId=byCode["WD-05"], RelationType="Prerequisite" },

                // HTTP/REST after Async
                new TrackPrerequisite{ TrackId=track.Id, UnitId=byCode["WD-07"], PrerequisiteUnitId=byCode["WD-06"], RelationType="Prerequisite" },

                // DB after HTTP
                new TrackPrerequisite{ TrackId=track.Id, UnitId=byCode["WD-08"], PrerequisiteUnitId=byCode["WD-07"], RelationType="Prerequisite" },

                // Back-end after DB
                new TrackPrerequisite{ TrackId=track.Id, UnitId=byCode["WD-09"], PrerequisiteUnitId=byCode["WD-08"], RelationType="Prerequisite" },

                // Deploy after back-end
                new TrackPrerequisite{ TrackId=track.Id, UnitId=byCode["WD-10"], PrerequisiteUnitId=byCode["WD-09"], RelationType="Prerequisite" },

                // Capstone after deploy
                new TrackPrerequisite{ TrackId=track.Id, UnitId=byCode["WD-11"], PrerequisiteUnitId=byCode["WD-10"], RelationType="Prerequisite" },
            };

            db.TrackPrerequisites.AddRange(prereqs);
            db.SaveChanges();
        }
    }
}

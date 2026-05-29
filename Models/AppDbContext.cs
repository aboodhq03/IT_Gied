using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IT_Gied.Models
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Course> Courses { get; set; }
        public DbSet<CoursePrerequisite> CoursePrerequisites { get; set; }
        public DbSet<UserCourseProgress> UserCourseProgresses { get; set; }
        public DbSet<Track> Tracks { get; set; }
        public DbSet<TrackNode> TrackNodes { get; set; }
        public DbSet<TrackLink> TrackLinks { get; set; }
        public DbSet<UserTrackProgress> UserTrackProgresses { get; set; }
        public DbSet<UserGpa> UserGpas { get; set; }
        public DbSet<AdvisorChatHistory> AdvisorChatHistories { get; set; }
        public DbSet<CourseRating> CourseRatings { get; set; }
        public DbSet<StudentBenefit> StudentBenefits { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CoursePrerequisite>(entity =>
            {
                entity.Property(x => x.RelationType)
                      .HasMaxLength(20)
                      .IsRequired();

                entity.HasOne(x => x.Course)
                      .WithMany(x => x.Prerequisites)
                      .HasForeignKey(x => x.CourseId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.PrerequisiteCourse)
                      .WithMany()
                      .HasForeignKey(x => x.PrerequisiteCourseId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => new { x.CourseId, x.PrerequisiteCourseId, x.RelationType })
                      .IsUnique();
            });

            modelBuilder.Entity<UserCourseProgress>(entity =>
            {
                entity.Property(x => x.UserId).IsRequired();

                entity.HasOne(x => x.Course)
                      .WithMany()
                      .HasForeignKey(x => x.CourseId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.User)
                      .WithMany()
                      .HasForeignKey(x => x.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => new { x.UserId, x.CourseId })
                      .IsUnique();
            });

         
            modelBuilder.Entity<UserGpa>(entity =>
            {
                entity.HasIndex(x => x.UserId).IsUnique();
            });

            modelBuilder.Entity<CourseRating>(entity =>
            {
                entity.HasIndex(x => new { x.UserId, x.CourseId }).IsUnique();
            });

            modelBuilder.Entity<StudentBenefit>(entity =>
            {
                entity.Property(x => x.Title)
                      .HasMaxLength(150)
                      .IsRequired();

                entity.Property(x => x.Category)
                      .HasMaxLength(100)
                      .IsRequired();

                entity.Property(x => x.ProviderName)
                      .HasMaxLength(100)
                      .IsRequired();

                entity.Property(x => x.Link)
                      .HasMaxLength(300)
                      .IsRequired();

                entity.Property(x => x.Icon)
                      .HasMaxLength(200);

                entity.Property(x => x.Description)
                      .HasMaxLength(1000);

                entity.Property(x => x.IsActive)
                      .HasDefaultValue(true);

                entity.Property(x => x.CreatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");

                entity.HasData(
                    new StudentBenefit
                    {
                        Id = 1,
                        Title = "GitHub Student Developer Pack",
                        Category = "Developer Tools",
                        ProviderName = "GitHub",
                        Link = "https://education.github.com/pack",
                        Icon = "fab fa-github",
                        Description = "Access free developer tools, cloud credits, and productivity services with your university email.",
                        IsActive = true,
                        CreatedAt = new DateTime(2026, 5, 28)
                    },
                    new StudentBenefit
                    {
                        Id = 2,
                        Title = "Notion Education Plus",
                        Category = "Learning Platforms",
                        ProviderName = "Notion",
                        Link = "https://www.notion.so/education",
                        Icon = "fas fa-book-open",
                        Description = "Upgrade your student workspace with Notion's premium features for notes, projects, and collaboration.",
                        IsActive = true,
                        CreatedAt = new DateTime(2026, 5, 28)
                    },
                    new StudentBenefit
                    {
                        Id = 3,
                        Title = "Canva for Education",
                        Category = "Design Tools",
                        ProviderName = "Canva",
                        Link = "https://www.canva.com/education/",
                        Icon = "fas fa-pencil-ruler",
                        Description = "Create presentations, social assets, and design collateral with premium Canva resources for students.",
                        IsActive = true,
                        CreatedAt = new DateTime(2026, 5, 28)
                    },
                    new StudentBenefit
                    {
                        Id = 4,
                        Title = "JetBrains Student License",
                        Category = "Developer Tools",
                        ProviderName = "JetBrains",
                        Link = "https://www.jetbrains.com/student/",
                        Icon = "fas fa-code",
                        Description = "Use the full JetBrains IDE suite for free while you're enrolled with a valid academic email address.",
                        IsActive = true,
                        CreatedAt = new DateTime(2026, 5, 28)
                    },
                    new StudentBenefit
                    {
                        Id = 5,
                        Title = "Microsoft Learn Student Hub",
                        Category = "AI & Cloud Credits",
                        ProviderName = "Microsoft",
                        Link = "https://learn.microsoft.com/student-hub/",
                        Icon = "fas fa-cloud",
                        Description = "Unlock student-focused learning paths, cloud credits, and AI productivity tools from Microsoft.",
                        IsActive = true,
                        CreatedAt = new DateTime(2026, 5, 28)
                    }
                );
            });
        }
    }
}

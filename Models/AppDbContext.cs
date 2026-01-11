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

        // ===== External tracks (WebDev, AI, ...) =====
        public DbSet<Track> Tracks { get; set; }
        public DbSet<TrackUnit> TrackUnits { get; set; }
        public DbSet<TrackPrerequisite> TrackPrerequisites { get; set; }
        public DbSet<UserTrackProgress> UserTrackProgresses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

           
            modelBuilder.Entity<CoursePrerequisite>(entity =>
            {
                entity.Property(x => x.RelationType)
                      .HasMaxLength(20)
                      .IsRequired();

                entity.HasOne(x => x.Course)
                      .WithMany()
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

            // ===== Track (external roadmap) =====
            modelBuilder.Entity<Track>(entity =>
            {
                entity.HasIndex(x => x.Slug).IsUnique();
            });

            modelBuilder.Entity<TrackUnit>(entity =>
            {
                entity.HasOne(x => x.Track)
                      .WithMany()
                      .HasForeignKey(x => x.TrackId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => new { x.TrackId, x.Code }).IsUnique();
            });

            modelBuilder.Entity<TrackPrerequisite>(entity =>
            {
                entity.Property(x => x.RelationType)
                      .HasMaxLength(20)
                      .IsRequired();

                entity.HasOne(x => x.Track)
                      .WithMany()
                      .HasForeignKey(x => x.TrackId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.Unit)
                      .WithMany()
                      .HasForeignKey(x => x.UnitId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.PrerequisiteUnit)
                      .WithMany()
                      .HasForeignKey(x => x.PrerequisiteUnitId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => new { x.TrackId, x.UnitId, x.PrerequisiteUnitId, x.RelationType })
                      .IsUnique();
            });

            modelBuilder.Entity<UserTrackProgress>(entity =>
            {
                entity.Property(x => x.UserId).IsRequired();

                entity.HasOne(x => x.TrackUnit)
                      .WithMany()
                      .HasForeignKey(x => x.TrackUnitId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.User)
                      .WithMany()
                      .HasForeignKey(x => x.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => new { x.UserId, x.TrackUnitId })
                      .IsUnique();
            });
        }
    }
}

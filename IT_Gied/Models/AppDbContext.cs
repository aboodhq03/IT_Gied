using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IT_Gied.Models
{
    public class AppDbContext : IdentityDbContext<IdentityUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Course> Courses { get; set; }
        public DbSet<CoursePrerequisite> CoursePrerequisites { get; set; }
        public DbSet<UserCourseProgress> UserCourseProgresses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // CoursePrerequisite - منع cascade paths
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

            // UserCourseProgress
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
                      .OnDelete(DeleteBehavior.Cascade); // إذا انحذف المستخدم تنحذف سجلات تقدمه

                entity.HasIndex(x => new { x.UserId, x.CourseId })
                      .IsUnique();
            });
        }
    }
}

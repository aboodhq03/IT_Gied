using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT_Gied.Models
{
    public class UserCourseProgress
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = default!;

        [Required]
        public int CourseId { get; set; }

        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }

        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; } 

        [ForeignKey(nameof(CourseId))]
        public Course? Course { get; set; }

    }
}

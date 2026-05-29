using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT_Gied.Models
{
    public class UserCourseProgress
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public int CourseId { get; set; }

        public bool IsCompleted { get; set; }

        public DateTime? CompletedAt { get; set; }

        [MaxLength(2)]
        public string? Grade { get; set; }

        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }

        [ForeignKey(nameof(CourseId))]
        public Course? Course { get; set; }

        public static double? LetterToPoint(string? letter)
        {
            if (string.IsNullOrWhiteSpace(letter))
                return null;

            return letter.Trim().ToUpper() switch
            {
                "A+" => 4.0,
                "A" => 4.0,
                "A-" => 3.7,
                "B+" => 3.3,
                "B" => 3.0,
                "B-" => 2.7,
                "C+" => 2.3,
                "C" => 2.0,
                "C-" => 1.7,
                "D+" => 1.3,
                "D" => 1.0,
                "D-" => 0.7,
                "F" => 0.0,
                _ => null
            };
        }
    }
}
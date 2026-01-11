using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT_Gied.Models
{
    public class CoursePrerequisite
    {
        public int Id { get; set; }

        [Required]
        public int CourseId { get; set; }

        [Required]
        public int PrerequisiteCourseId { get; set; }

        [Required, MaxLength(20)]
        public string RelationType { get; set; } = "Prerequisite";

        [ForeignKey(nameof(CourseId))]
        public Course? Course { get; set; }

        [ForeignKey(nameof(PrerequisiteCourseId))]
        public Course? PrerequisiteCourse { get; set; }
    }
}

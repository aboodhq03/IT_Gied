using System.ComponentModel.DataAnnotations;

namespace IT_Gied.Models
{
    public class Course
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; }

        [Required, MaxLength(50)]
        public string Code { get; set; }

        public string? Description { get; set; }

        [Range(0, 30)]
        public int CreditHours { get; set; }

        [MaxLength(500)]
        public string? DetailsUrl { get; set; }

        public string? EDetailsUrl { get; set; }

    }
}

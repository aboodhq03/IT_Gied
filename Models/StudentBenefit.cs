using System;
using System.ComponentModel.DataAnnotations;

namespace IT_Gied.Models
{
    public class StudentBenefit
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string ProviderName { get; set; } = string.Empty;

        [Required, MaxLength(300), Url]
        public string Link { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Icon { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

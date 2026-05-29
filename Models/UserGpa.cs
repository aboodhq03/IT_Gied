using System.ComponentModel.DataAnnotations.Schema;

namespace IT_Gied.Models
{
    public class UserGpa
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18, 2)")]
        public decimal CumulativeGpa { get; set; }

        public DateTime UpdatedAt { get; set; }
    }

}

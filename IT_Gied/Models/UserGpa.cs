namespace IT_Gied.Models
{
    public class UserGpa
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;

        public decimal CumulativeGpa { get; set; }

        public DateTime UpdatedAt { get; set; }
    }

}

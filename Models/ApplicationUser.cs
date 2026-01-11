using Microsoft.AspNetCore.Identity;

namespace IT_Gied.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? Image_Name { get; set; }
    }
}

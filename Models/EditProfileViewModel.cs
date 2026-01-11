using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace IT_Gied.Models
{
    public class EditProfileViewModel
    {
        [EmailAddress]
        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

      
        public string? CurrentImageName { get; set; }

      
        public IFormFile? ImageFile { get; set; }
    }
}

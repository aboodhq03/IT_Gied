using System.ComponentModel.DataAnnotations;

namespace IT_Gied.Models
{
    public class EditProfileViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; }

        [Phone]
        public string? PhoneNumber { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace IT_Gied.Models
{
    /// <summary>
    /// Model used for user registration.
    /// الموديل المستخدم لعملية تسجيل مستخدم جديد.
    /// </summary>
    public class RegisterModel
    {
        [Required(ErrorMessage = "User Name is required")]
        [Display(Name = "User Name")]
        public string? UserName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Display(Name = "Password")]
        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [Display(Name = "Confirm Password")]
        [Required(ErrorMessage = "Confirm Password is required")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Password and Confirm Password do not match")]
        public string? ConfirmPassword { get; set; }

        // Optional profile image
        // صورة اختيارية للملف الشخصي
        public IFormFile? ImageFile { get; set; }
    }
}

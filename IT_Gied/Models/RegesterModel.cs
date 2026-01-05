using System.ComponentModel.DataAnnotations;

namespace IT_Gied.Models
{
    public class RegesterModel
    {
        [Required(ErrorMessage = "This fild is required")]
        [Display(Name = "User Name")]
        public string User_name { get; set; }
        [EmailAddress]
        public string User_email { get; set; }
        [Display(Name = "password")]
        [Required]
        [DataType(DataType.Password)]
        [Compare("confirm_Password")]
        public string Password { get; set; }
        [Display(Name = "confirm password")]
        [Required]
        [DataType(DataType.Password)]
        public string confirm_Password { get; set; }
    }
}

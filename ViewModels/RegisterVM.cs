using System.ComponentModel.DataAnnotations;

namespace AspiraHub.ViewModels
{
    public class RegisterVM
    {
        [Required(ErrorMessage = "Full name required.")]
        [MaxLength(100)]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "Email required.")]
        [EmailAddress(ErrorMessage = "Invalid email.")]
        [MaxLength(100)]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Password required.")]
        [MinLength(6, ErrorMessage = "Minimum 6 characters.")]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Confirm password required.")]
        public string ConfirmPassword { get; set; } = "";

        public string Role { get; set; } = "Student";
    }
}
using System.ComponentModel.DataAnnotations;

namespace AspiraHub.ViewModels
{
    public class ForgotPasswordVM
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email.")]
        public string Email { get; set; }

        // Step 2 — OTP verify
        public string? OtpCode { get; set; }

        // Step 3 — New password
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
        public string? NewPassword { get; set; }

        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        public string? ConfirmNewPassword { get; set; }

        // Track which step user is on: 1=Email, 2=OTP, 3=NewPassword
        public int Step { get; set; } = 1;
    }
}
using System.ComponentModel.DataAnnotations;

namespace AspiraHub.ViewModels
{
    public class LoginVM
    {
        // ASP-XXXX-XXXX for Student, Email for Company/Admin
        [Required(ErrorMessage = "Please enter your key or email.")]
        public string LoginIdentifier { get; set; }

        [Required, DataType(DataType.Password)]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }
}
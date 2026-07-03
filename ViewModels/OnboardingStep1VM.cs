using System.ComponentModel.DataAnnotations;

namespace AspiraHub.ViewModels
{
    public class OnboardingStep1VM
    {
        [Required(ErrorMessage = "Please select education level.")]
        public string EducationLevel { get; set; }
        // Intermediate / Undergraduate / Graduate
    }
}
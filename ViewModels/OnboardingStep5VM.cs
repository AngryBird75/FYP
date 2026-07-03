using System.ComponentModel.DataAnnotations;

namespace AspiraHub.ViewModels
{
    public class OnboardingStep5VM
    {
        [Required(ErrorMessage = "Please select your main goal.")]
        public string Goal { get; set; }
        // Get a Job / Freelancing / Higher Education / Start a Business / Just Learning
    }
}
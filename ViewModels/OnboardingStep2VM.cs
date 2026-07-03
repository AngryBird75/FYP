using System.ComponentModel.DataAnnotations;

namespace AspiraHub.ViewModels
{
    public class OnboardingStep2VM
    {
        // Ab free text nahi — Universities table se dropdown select hota hai,
        // isliye sirf DB waali valid university hi select ho sakti hai.
        [Required(ErrorMessage = "Please select your university.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select your university.")]
        public int UniversityId { get; set; }

        [Required, MaxLength(100)]
        public string Program { get; set; }

        [Required, MaxLength(100)]
        public string Major { get; set; }

        public List<UniversityOption> AvailableUniversities { get; set; } = new();
    }

    public class UniversityOption
    {
        public int UniversityId { get; set; }
        public string Name { get; set; } = "";
        public string Location { get; set; } = "";
    }
}

using System.ComponentModel.DataAnnotations;

namespace AspiraHub.ViewModels
{
    public class OnboardingStep1BVM
    {
        [Required(ErrorMessage = "Please select your degree program")]
        public int DegreeProgramId { get; set; }

        [Required(ErrorMessage = "Please select your current semester")]
        [Range(1, 8, ErrorMessage = "Semester must be between 1 and 8")]
        public int CurrentSemester { get; set; }

        public List<DegreeProgramOption> AvailablePrograms { get; set; } = new();
    }

    public class DegreeProgramOption
    {
        public int ProgramId { get; set; }
        public string ProgramName { get; set; } = "";
        public string FullName { get; set; } = "";
        public int TotalSemesters { get; set; }
    }
}
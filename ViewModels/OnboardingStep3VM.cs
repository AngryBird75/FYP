namespace AspiraHub.ViewModels
{
    public class OnboardingStep3VM
    {
        public List<SkillItemVM> Skills { get; set; } = new();
    }

    public class SkillItemVM
    {
        public string SkillName { get; set; } = "";
        public string SkillLevel { get; set; } = "";
    }
}
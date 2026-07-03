namespace AspiraHub.ViewModels.Career
{
    public class SkillGapVM
    {
        public int CareerId { get; set; }
        public string CareerTitle { get; set; } = "";
        public int MatchPercent { get; set; }

        public List<SkillStatusVM> HaveSkills { get; set; } = new();
        public List<SkillStatusVM> MissingSkills { get; set; } = new();

        public List<string> SuggestedCourses { get; set; } = new();
    }

    public class SkillStatusVM
    {
        public string SkillName { get; set; } = "";
        public string ImportanceLevel { get; set; } = "";
        public string? CurrentLevel { get; set; }
        public string? GapLevel { get; set; }
    }
}
namespace AspiraHub.DTOs
{
    // Matches the Android app's CareerDto exactly (field-for-field) —
    // Views/Career/Explore.cshtml + Views/Career/SavedCareers.cshtml.
    public class CareerDto
    {
        public int careerId { get; set; }
        public string title { get; set; } = "";
        public string category { get; set; } = "";
        public int matchScore { get; set; }
        public string avgSalary { get; set; } = "";
        public string growthOutlook { get; set; } = "";
        public string description { get; set; } = "";
        public bool isSaved { get; set; }
    }

    // Matches AspiraHub.ViewModels.Career.SkillGapVM exactly (confirmed
    // against Views/Career/SkillGap.cshtml).
    public class SkillGapResponse
    {
        public string careerTitle { get; set; } = "";
        public int matchPercent { get; set; }
        public List<SkillGapSkillDto> haveSkills { get; set; } = new();
        public List<SkillGapSkillDto> partialSkills { get; set; } = new();
        public List<SkillGapSkillDto> missingSkills { get; set; } = new();
        public List<SuggestedCourseDto> suggestedCourses { get; set; } = new();
    }

    public class SkillGapSkillDto
    {
        public string skillName { get; set; } = "";
        public string importanceLevel { get; set; } = "";  // Critical / High / Medium / Low
        public string currentLevel { get; set; } = "";     // blank for missing skills
        public string expectedLevel { get; set; } = "";    // only set for partial skills
    }

    public class SuggestedCourseDto
    {
        public int? courseId { get; set; }
        public string text { get; set; } = "";
    }
}

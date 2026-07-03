namespace AspiraHub.Services
{
    public static class RoadmapPhaseGrouper
    {
        public static readonly List<string> PhaseOrder = new()
        {
            "Getting Started", "Academic Path", "Skill Assessment",
            "Skill Building", "Portfolio & Practice", "Job Readiness"
        };

        private static readonly Dictionary<string, string> Descriptions = new()
        {
            ["Getting Started"] = "Aapke roadmap ka overview aur pehla step.",
            ["Academic Path"] = "Apni degree ke academic milestones semester-wise complete karo.",
            ["Skill Assessment"] = "Dekho konsi skills already hain aur konsi missing hain.",
            ["Skill Building"] = "Is career ke liye zaroori missing skills seekho.",
            ["Portfolio & Practice"] = "Jo seekha usay real project(s) mein apply karo.",
            ["Job Readiness"] = "Internships aur jobs ke liye apply karna shuru karo."
        };

        private static readonly Dictionary<string, string> Icons = new()
        {
            ["Getting Started"] = "🚀",
            ["Academic Path"] = "🎓",
            ["Skill Assessment"] = "🔍",
            ["Skill Building"] = "🛠️",
            ["Portfolio & Practice"] = "💼",
            ["Job Readiness"] = "🎯"
        };

        public static string GetPhaseName(string stepTitle)
        {
            if (stepTitle.StartsWith("Semester") || stepTitle == "Complete Your Academic Requirements")
                return "Academic Path";

            if (stepTitle is "Add Your Skills to Your Profile" or "Skills You Already Bring"
                || stepTitle.StartsWith("Research"))
                return "Skill Assessment";

            if (stepTitle.StartsWith("Learn "))
                return "Skill Building";

            if (stepTitle is "Build a Portfolio Project" or "You're Skill-Ready")
                return "Portfolio & Practice";

            if (stepTitle is "Find an Internship" or "Start Applying for Jobs")
                return "Job Readiness";

            return "Getting Started";
        }

        public static string GetDescription(string phase) => Descriptions.GetValueOrDefault(phase, "");
        public static string GetIcon(string phase) => Icons.GetValueOrDefault(phase, "📌");
    }
}
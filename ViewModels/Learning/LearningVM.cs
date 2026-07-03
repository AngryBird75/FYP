namespace AspiraHub.ViewModels.Learning
{
    public class RecommendedCoursesVM
    {
        public List<RecommendedCourseVM> Courses { get; set; } = new();
    }

    public class RecommendedCourseVM
    {
        public int CourseId { get; set; }
        public string CourseName { get; set; } = "";
        public string InstituteName { get; set; } = "";
        public string Duration { get; set; } = "";
        public string Fee { get; set; } = "";
        public string Reason { get; set; } = "";
        // NEW: Institutes.type se — "Online" / "Physical" / "Hybrid"
        public string Mode { get; set; } = "";
    }

    public class UniversityRecsVM
    {
        public List<UniversityRecVM> Universities { get; set; } = new();
    }

    public class UniversityRecVM
    {
        public int UniversityId { get; set; }
        public string Name { get; set; } = "";
        public string Location { get; set; } = "";
        public int? Ranking { get; set; }
        public string Type { get; set; } = "";
        public string FeeStructure { get; set; } = "";
        public string Reason { get; set; } = "";
    }

    public class MyLearningProgressVM
    {
        public int CoursesInProgress { get; set; }
        public int CoursesCompleted { get; set; }
        public int TotalRoadmaps { get; set; }
        public int RoadmapStepsTotal { get; set; }
        public int RoadmapStepsDone { get; set; }
        public int OverallProgressPercent { get; set; }
    }
}
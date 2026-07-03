namespace AspiraHub.ViewModels.Roadmap
{
    public class RoadmapListVM
    {
        public List<RoadmapCardVM> Roadmaps { get; set; } = new();
    }

    public class RoadmapCardVM
    {
        public int RoadmapId { get; set; }
        public string Title { get; set; } = "";
        public string Type { get; set; } = "";
        public string Status { get; set; } = "";
        public int TotalSteps { get; set; }
        public int CompletedSteps { get; set; }
        public int ProgressPercent { get; set; }
        public DateTime CreatedAt { get; set; }
        public int DurationMonths { get; set; }
        public DateTime? TargetEndDate { get; set; }
        public int? DaysRemaining => TargetEndDate.HasValue
            ? (int?)Math.Ceiling((TargetEndDate.Value - DateTime.Now).TotalDays)
            : null;
    }

    public class RoadmapDetailVM
    {
        public int RoadmapId { get; set; }
        public string Title { get; set; } = "";
        public string Type { get; set; } = "";
        public string Status { get; set; } = "";
        public int ProgressPercent { get; set; }
        public int DurationMonths { get; set; }
        public DateTime? TargetEndDate { get; set; }
        public int? DaysRemaining => TargetEndDate.HasValue
            ? (int?)Math.Ceiling((TargetEndDate.Value - DateTime.Now).TotalDays)
            : null;
        public List<RoadmapStepVM> Steps { get; set; } = new();
        public List<RoadmapPhaseVM> Phases { get; set; } = new();
    }

    public class RoadmapPhaseVM
    {
        public string PhaseName { get; set; } = "";
        public string PhaseStatus { get; set; } = "Locked";
        public string Description { get; set; } = "";
        public string Icon { get; set; } = "📌";
        public DateTime? TargetDate { get; set; }   // NEW — duration ke hisaab se is phase ki deadline
        public List<RoadmapStepVM> Steps { get; set; } = new();

        public int TotalSteps => Steps.Count;
        public int CompletedSteps => Steps.Count(s => s.ProgressStatus == "Completed");
    }

    public class RoadmapStepVM
    {
        public int StepId { get; set; }
        public string StepTitle { get; set; } = "";
        public string? Description { get; set; }
        public int StepOrder { get; set; }
        public string? ResourceUrl { get; set; }
        public string ProgressStatus { get; set; } = "Pending";
    }

    public class GenerateRoadmapVM
    {
        public int CareerId { get; set; }
        public string RoadmapType { get; set; } = "Mixed";     // Career / Education / Mixed
        public int DurationMonths { get; set; } = 6;           // 3 / 6 / 9 / 12 / 18 / 24
        public List<CareerOptionForRoadmap> AvailableCareers { get; set; } = new();
    }

    public class CareerOptionForRoadmap
    {
        public int CareerId { get; set; }
        public string Title { get; set; } = "";
    }

    public class RoadmapReportVM
    {
        public int TotalRoadmaps { get; set; }
        public int ActiveRoadmaps { get; set; }
        public int CompletedRoadmaps { get; set; }
        public int PausedRoadmaps { get; set; }
        public int TotalStepsAcrossAll { get; set; }
        public int CompletedStepsAcrossAll { get; set; }
        public int OverallProgressPercent { get; set; }
        public List<RoadmapCardVM> RoadmapBreakdown { get; set; } = new();
    }

    // ── NEW: "Details" button ke liye — konsa institute/university ye skill/course deta hai ──
    public class StepResourcesVM
    {
        public string StepTitle { get; set; } = "";
        public List<CourseResourceVM> Courses { get; set; } = new();
        public List<UniversityResourceVM> Universities { get; set; } = new();
    }

    public class CourseResourceVM
    {
        public string CourseName { get; set; } = "";
        public string InstituteName { get; set; } = "";
        public string Duration { get; set; } = "";
        public string Fee { get; set; } = "";
        public string? Website { get; set; }
        // NEW: Institutes.type se aata hai — "Online" / "Physical" / "Hybrid"
        public string Mode { get; set; } = "";
    }

    public class UniversityResourceVM
    {
        public string Name { get; set; } = "";
        public string Location { get; set; } = "";
        public int? Ranking { get; set; }
        public string? Website { get; set; }
    }
}
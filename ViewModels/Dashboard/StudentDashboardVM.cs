using AspiraHub.Models;

namespace AspiraHub.ViewModels.Dashboard
{
    public class StudentDashboardVM
    {
        // Profile
        public string Name { get; set; } = "";
        public string UniqueKey { get; set; } = "";
        public string EducationLevel { get; set; } = "";
        public string UniversityName { get; set; } = "";
        public string Program { get; set; } = "";
        public string Interests { get; set; } = "";
        public string Goal { get; set; } = "";
        public int ProfileCompletion { get; set; }
        public string? ProfilePicture { get; set; }

        // Skills
        public List<StudentSkillVM> Skills { get; set; } = new();

        // Courses
        public int CoursesInProgress { get; set; }
        public int CoursesCompleted { get; set; }

        // Roadmaps
        public int TotalRoadmaps { get; set; }
        public int RoadmapProgress { get; set; }

        // Jobs
        public int MatchedJobs { get; set; }
        public int AppliedJobs { get; set; }
        public List<MatchedJobVM> TopMatchedJobs { get; set; } = new();

        // Notifications
        public int UnreadNotifications { get; set; }
        public List<Notification> RecentNotifications { get; set; } = new();

        // Announcements
        public List<Announcement> Announcements { get; set; } = new();
    }

    public class StudentSkillVM
    {
        public string SkillName { get; set; } = "";
        public string ProficiencyLevel { get; set; } = "";
    }

    public class MatchedJobVM
    {
        public int JobId { get; set; }
        public string Title { get; set; } = "";
        public string Company { get; set; } = "";
        public string Location { get; set; } = "";
        public int MatchScore { get; set; }
        public string JobType { get; set; } = "";
        public DateTime? Deadline { get; set; }
    }
}
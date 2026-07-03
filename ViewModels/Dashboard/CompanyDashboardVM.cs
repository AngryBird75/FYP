using AspiraHub.Models;

namespace AspiraHub.ViewModels.Dashboard
{
    public class CompanyDashboardVM
    {
        // Company Info
        public string CompanyName { get; set; } = "";
        public string Industry { get; set; } = "";
        public string Location { get; set; } = "";
        public string? LogoUrl { get; set; }
        public bool IsVerified { get; set; }

        // Stats
        public int TotalJobs { get; set; }
        public int ActiveJobs { get; set; }
        public int TotalApplications { get; set; }
        public int PendingApplications { get; set; }
        public int TotalViews { get; set; }

        // Jobs List
        public List<CompanyJobVM> Jobs { get; set; } = new();

        // Recent Applications
        public List<RecentApplicationVM> RecentApplications { get; set; } = new();

        // Notifications
        public int UnreadNotifications { get; set; }
        public List<Notification> RecentNotifications { get; set; } = new();
    }

    public class CompanyJobVM
    {
        public int JobId { get; set; }
        public string Title { get; set; } = "";
        public string Location { get; set; } = "";
        public string JobType { get; set; } = "";
        public string Status { get; set; } = "";
        public int Views { get; set; }
        public int Applications { get; set; }
        public DateTime? Deadline { get; set; }
        public DateTime PostedDate { get; set; }
    }

    public class RecentApplicationVM
    {
        public int ApplicationId { get; set; }
        public string StudentName { get; set; } = "";
        public string JobTitle { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime AppliedAt { get; set; }
        public string? ResumeUrl { get; set; }
    }
}
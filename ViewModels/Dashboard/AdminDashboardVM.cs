using AspiraHub.Models;

namespace AspiraHub.ViewModels.Dashboard
{
    public class AdminDashboardVM
    {
        // Platform Stats
        public int TotalUsers { get; set; }
        public int TotalStudents { get; set; }
        public int TotalCompanies { get; set; }
        public int TotalJobs { get; set; }
        public int TotalApplications { get; set; }
        public int TotalCareers { get; set; }
        public int TotalSkills { get; set; }
        public int ActiveAnnouncements { get; set; }

        // Recent Users
        public List<RecentUserVM> RecentUsers { get; set; } = new();

        // Recent Jobs
        public List<AdminJobVM> RecentJobs { get; set; } = new();

        // Announcements
        public List<Announcement> Announcements { get; set; } = new();

        // Admin Logs
        public List<AdminLog> RecentLogs { get; set; } = new();

        // Role Distribution
        public int StudentCount { get; set; }
        public int CompanyCount { get; set; }
        public int AdminCount { get; set; }
    }

    public class RecentUserVM
    {
        public int UserId { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Role { get; set; } = "";
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AdminJobVM
    {
        public int JobId { get; set; }
        public string Title { get; set; } = "";
        public string Company { get; set; } = "";
        public string Status { get; set; } = "";
        public int Applications { get; set; }
        public DateTime PostedDate { get; set; }
    }
}
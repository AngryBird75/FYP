using AspiraHub.Models;
using AspiraHub.ViewModels.Dashboard;

namespace AspiraHub.Repositories
{
    public interface IDashboardRepository
    {
        // Student
        Task<StudentDashboardVM> GetStudentDashboardAsync(int userId);

        // Company
        Task<CompanyDashboardVM> GetCompanyDashboardAsync(int userId);

        // Admin
        Task<AdminDashboardVM> GetAdminDashboardAsync();

        // Shared
        Task<List<Notification>> GetNotificationsAsync(int userId, int take = 5);
        Task MarkNotificationReadAsync(int notifId, int userId);
        Task<int> GetUnreadCountAsync(int userId);
    }
}
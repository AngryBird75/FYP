using AspiraHub.ViewModels.Job;

namespace AspiraHub.Services
{
    public interface IJobService
    {
        Task<JobBrowseVM> BrowseJobsAsync(int studentId, string? search, string? jobType);
        Task<List<JobCardVM>> GetSavedJobsAsync(int userId, int studentId);
        Task<List<MyApplicationVM>> GetMyApplicationsAsync(int studentId);
        Task<bool> ApplyJobAsync(int studentId, int jobId, string? coverLetter);
        Task<bool> SaveJobAsync(int userId, int jobId);
        Task<bool> UnsaveJobAsync(int userId, int jobId);
        Task RunMatchingForStudentAsync(int studentId);
    }
}
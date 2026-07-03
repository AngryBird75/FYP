using AspiraHub.ViewModels.Learning;

namespace AspiraHub.Services
{
    public interface ILearningService
    {
        Task<RecommendedCoursesVM> GetRecommendedCoursesAsync(int studentId);
        Task<UniversityRecsVM> GetUniversityRecommendationsAsync(int studentId);
        Task<MyLearningProgressVM> GetMyProgressAsync(int studentId);
    }
}
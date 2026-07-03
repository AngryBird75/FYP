using AspiraHub.ViewModels.Career;

namespace AspiraHub.Services
{
    public interface ICareerService
    {
        Task<CareerExploreVM> ExploreCareersAsync(int studentId, string? search, string? demandFilter);
        Task<SkillGapVM> AnalyzeSkillGapAsync(int studentId, int careerId);
        Task<CareerCompareVM> GetCompareOptionsAsync();
        Task<CareerCompareVM> CompareCareersAsync(int studentId, int careerIdA, int careerIdB);
        Task<bool> SaveCareerAsync(int userId, int careerId);
        Task<bool> UnsaveCareerAsync(int userId, int careerId);
        Task<List<CareerCardVM>> GetSavedCareersAsync(int userId, int studentId);
        Task<List<CareerCardVM>> GetRecommendedCareersAsync(int studentId, int take = 5);
    }
}
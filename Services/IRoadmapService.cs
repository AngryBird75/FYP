using AspiraHub.ViewModels.Roadmap;

namespace AspiraHub.Services
{
    public interface IRoadmapService
    {
        Task<List<CareerOptionForRoadmap>> GetCareerOptionsAsync();
        Task<int> GenerateRoadmapAsync(int studentId, GenerateRoadmapVM vm);
        Task<RoadmapListVM> GetMyRoadmapsAsync(int studentId);
        Task<RoadmapDetailVM?> GetRoadmapDetailAsync(int roadmapId, int studentId);
        Task<bool> UpdateStepStatusAsync(int stepId, int studentId, string newStatus);
        Task<bool> DeleteRoadmapAsync(int roadmapId, int studentId);
        Task<bool> PauseRoadmapAsync(int roadmapId, int studentId);
        Task<bool> ResumeRoadmapAsync(int roadmapId, int studentId);
        Task<bool> UpdateRoadmapTitleAsync(int roadmapId, int studentId, string newTitle);
        Task<RoadmapReportVM> GenerateReportAsync(int studentId);
        Task<StepResourcesVM> GetStepResourcesAsync(int stepId, int studentId);
    }
}
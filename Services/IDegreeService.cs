using AspiraHub.Models;
using AspiraHub.ViewModels;

namespace AspiraHub.Services
{
    public interface IDegreeService
    {
        Task<List<DegreeProgramOption>> GetActiveProgramsAsync();
        Task<bool> SaveStep1BAsync(int userId, OnboardingStep1BVM vm);
        Task<DegreeProgram?> GetStudentProgramAsync(int studentId);
    }
}
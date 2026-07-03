using AspiraHub.Models;

namespace AspiraHub.Services
{
    public interface ISkillCatalogService
    {
        Task<List<string>> GetAllSkillNamesAsync();
        Task<Skill?> FindByNameAsync(string name);

        // NEW: sirf us degree program se relevant skills — Step3 (Skills)
        // ab poori catalog ke bajaye sirf student ke department ki
        // skills dikhata hai. Agar us program ke liye mapping abhi
        // set nahi hui, poori catalog fallback ke taur pe milti hai.
        Task<List<string>> GetSkillNamesByProgramAsync(int programId);
    }
}

using AspiraHub.Data;
using AspiraHub.Models;
using Microsoft.EntityFrameworkCore;

namespace AspiraHub.Services
{
    // Skills table hi single source of truth hai — Step3 ka dropdown
    // aur server-side validation dono isi se aate hain, koi free text allowed nahi
    public class SkillCatalogService : ISkillCatalogService
    {
        private readonly AppDbContext _db;
        public SkillCatalogService(AppDbContext db) => _db = db;

        public async Task<List<string>> GetAllSkillNamesAsync()
            => await _db.Skills
                .OrderBy(s => s.skill_name)
                .Select(s => s.skill_name)
                .ToListAsync();

        public async Task<Skill?> FindByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            string normalized = name.Trim().ToLower();
            return await _db.Skills
                .FirstOrDefaultAsync(s => s.skill_name.ToLower() == normalized);
        }

        // NEW — ProgramSkills table se sirf us program/department ki skills lao.
        // Agar admin ne abhi us program ke liye koi skill map nahi ki, poori
        // catalog fallback ke taur pe wapas kar dete hain (taake Step3 khali na ho).
        public async Task<List<string>> GetSkillNamesByProgramAsync(int programId)
        {
            var names = await _db.ProgramSkills
                .Where(ps => ps.program_id == programId)
                .Include(ps => ps.Skill)
                .Where(ps => ps.Skill != null)
                .Select(ps => ps.Skill!.skill_name)
                .Distinct()
                .OrderBy(n => n)
                .ToListAsync();

            return names.Any() ? names : await GetAllSkillNamesAsync();
        }
    }
}

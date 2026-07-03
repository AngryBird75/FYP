using AspiraHub.Data;
using AspiraHub.Models;
using AspiraHub.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AspiraHub.Services
{
    public class DegreeService : IDegreeService
    {
        private readonly AppDbContext _db;

        public DegreeService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<DegreeProgramOption>> GetActiveProgramsAsync()
        {
            return await _db.DegreePrograms
                .Where(p => p.is_active)
                .OrderBy(p => p.education_level)
                .ThenBy(p => p.program_name)
                .Select(p => new DegreeProgramOption
                {
                    ProgramId = p.program_id,
                    ProgramName = p.program_name,
                    FullName = p.full_name ?? p.program_name,
                    TotalSemesters = p.total_semesters
                })
                .ToListAsync();
        }

        public async Task<bool> SaveStep1BAsync(int userId, OnboardingStep1BVM vm)
        {
            var profile = await _db.StudentProfiles
                .FirstOrDefaultAsync(s => s.user_id == userId);

            if (profile == null) return false;

            profile.degree_program_id = vm.DegreeProgramId;
            profile.current_semester = vm.CurrentSemester;

            // also fill the program field with the program name (for display)
            var program = await _db.DegreePrograms.FindAsync(vm.DegreeProgramId);
            if (program != null)
                profile.program = program.program_name;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<DegreeProgram?> GetStudentProgramAsync(int studentId)
        {
            var profile = await _db.StudentProfiles.FindAsync(studentId);
            if (profile?.degree_program_id == null) return null;

            return await _db.DegreePrograms.FindAsync(profile.degree_program_id.Value);
        }
    }
}
using AspiraHub.Data;
using AspiraHub.Models;
using Microsoft.EntityFrameworkCore;

namespace AspiraHub.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _db;

        public UserRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<User?> GetByIdAsync(int userId)
            => await _db.Users
                        .Include(u => u.StudentProfile)
                        .Include(u => u.CompanyProfile)
                        .FirstOrDefaultAsync(u => u.user_id == userId);

        public async Task<User?> GetByEmailAsync(string email)
            => await _db.Users
                        .FirstOrDefaultAsync(u => u.email == email.ToLower());

        public async Task<User?> GetByUniqueKeyAsync(string uniqueKey)
            => await _db.Users
                        .Include(u => u.StudentProfile)
                        .FirstOrDefaultAsync(u => u.unique_key == uniqueKey.ToUpper());

        public async Task<bool> EmailExistsAsync(string email)
            => await _db.Users.AnyAsync(u => u.email == email.ToLower());

        public async Task<bool> UniqueKeyExistsAsync(string key)
        {
            var upperKey = key.ToUpper();
            var anyUser = await _db.Users.AnyAsync();
            if (!anyUser) return false;

            var allKeys = await _db.Users.Select(u => u.unique_key).ToListAsync();
            return allKeys.Any(k => k != null && k.ToUpper() == upperKey);
        }

        public async Task<User> CreateAsync(User user)
        {
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return user;
        }

        public async Task UpdateAsync(User user)
        {
            _db.Users.Update(user);
            await _db.SaveChangesAsync();
        }

        // ── Student Profile ───────────────────────────────────────────

        public async Task<StudentProfile> CreateStudentProfileAsync(StudentProfile profile)
        {
            _db.StudentProfiles.Add(profile);
            await _db.SaveChangesAsync();
            return profile;
        }

        public async Task UpdateStudentProfileAsync(StudentProfile profile)
        {
            _db.StudentProfiles.Update(profile);
            await _db.SaveChangesAsync();
        }

        // 🔧 BUG FIX: pehle `skill.student_id = userId` set ho raha tha
        // (User.user_id ≠ StudentProfile.student_id) — is wajah se
        // RoadmapService kabhi bhi skills match nahi kar pata tha.
        // Ab caller (AuthService) StudentProfile.student_id bhejta hai.
        public async Task SaveStudentSkillsAsync(int studentId, List<StudentSkill> skills)
        {
            var old = _db.StudentSkills.Where(s => s.student_id == studentId);
            _db.StudentSkills.RemoveRange(old);

            foreach (var skill in skills)
            {
                skill.student_id = studentId;
                _db.StudentSkills.Add(skill);
            }

            await _db.SaveChangesAsync();
        }

        public async Task<StudentProfile?> GetStudentProfileAsync(int userId)
            => await _db.StudentProfiles
                        .FirstOrDefaultAsync(s => s.user_id == userId);

        // ── Company Profile ───────────────────────────────────────────

        public async Task<CompanyProfile> CreateCompanyProfileAsync(CompanyProfile profile)
        {
            _db.CompanyProfiles.Add(profile);
            await _db.SaveChangesAsync();
            return profile;
        }

        public async Task UpdateCompanyProfileAsync(CompanyProfile profile)
        {
            _db.CompanyProfiles.Update(profile);
            await _db.SaveChangesAsync();
        }

        public async Task<CompanyProfile?> GetCompanyProfileAsync(int userId)
            => await _db.CompanyProfiles
                        .FirstOrDefaultAsync(c => c.user_id == userId);

        // ── Password Reset (OTP) ────────────────────────────────────

        public async Task<PasswordReset> CreatePasswordResetAsync(PasswordReset reset)
        {
            _db.PasswordResets.Add(reset);
            await _db.SaveChangesAsync();
            return reset;
        }

        // Matches the newest, unused, non-expired OTP for this user.
        public async Task<PasswordReset?> GetValidResetAsync(int userId, string otp)
            => await _db.PasswordResets
                        .Where(r => r.user_id == userId
                                 && r.otp_code == otp
                                 && !r.used
                                 && r.expires_at > DateTime.Now)
                        .OrderByDescending(r => r.created_at)
                        .FirstOrDefaultAsync();

        public async Task MarkResetUsedAsync(PasswordReset reset)
        {
            reset.used = true;
            _db.PasswordResets.Update(reset);
            await _db.SaveChangesAsync();
        }
    }
}
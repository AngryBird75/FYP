using AspiraHub.Models;

namespace AspiraHub.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(int userId);
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByUniqueKeyAsync(string uniqueKey);
        Task<bool> EmailExistsAsync(string email);
        Task<bool> UniqueKeyExistsAsync(string key);
        Task<User> CreateAsync(User user);
        Task SaveStudentSkillsAsync(int studentId, List<StudentSkill> skills);
        Task UpdateAsync(User user);
        Task<StudentProfile> CreateStudentProfileAsync(StudentProfile profile);
        Task UpdateStudentProfileAsync(StudentProfile profile);
        Task<CompanyProfile> CreateCompanyProfileAsync(CompanyProfile profile);
        Task UpdateCompanyProfileAsync(CompanyProfile profile);
        Task<StudentProfile?> GetStudentProfileAsync(int userId);
        Task<CompanyProfile?> GetCompanyProfileAsync(int userId);

        // ── Password Reset (OTP) ────────────────────────────────────
        Task<PasswordReset> CreatePasswordResetAsync(PasswordReset reset);
        Task<PasswordReset?> GetValidResetAsync(int userId, string otp);
        Task MarkResetUsedAsync(PasswordReset reset);
    }
}
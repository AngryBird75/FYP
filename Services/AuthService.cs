using AspiraHub.Data;
using AspiraHub.Models;
using AspiraHub.Repositories;
using AspiraHub.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AspiraHub.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _repo;
        private readonly UniqueKeyService _keyService;
        private readonly ISkillCatalogService _skillCatalog;
        private readonly IEmailService _email;
        private readonly AppDbContext _db;
        private readonly int _otpExpiryMinutes;
        private static readonly Random _rng = new Random();

        public AuthService(IUserRepository repo, UniqueKeyService keyService, ISkillCatalogService skillCatalog, IEmailService email, AppDbContext db, IConfiguration config)
        {
            _repo = repo;
            _keyService = keyService;
            _skillCatalog = skillCatalog;
            _email = email;
            _db = db;
            _otpExpiryMinutes = config.GetValue<int?>("AppSettings:OtpExpiryMinutes") ?? 10;
        }

        // ── Register ──────────────────────────────────────────────────
        public async Task<(bool, string, User?)> RegisterAsync(RegisterVM vm)
        {
            if (await _repo.EmailExistsAsync(vm.Email))
                return (false, "Email already registered.", null);

            var uniqueKey = await _keyService.GenerateAsync();

            var user = new User
            {
                name = vm.FullName,
                email = vm.Email.ToLower().Trim(),
                hashed_password = BCrypt.Net.BCrypt.HashPassword(vm.Password),
                role = vm.Role,
                unique_key = uniqueKey,
                is_active = true,
                created_at = DateTime.Now
            };

            await _repo.CreateAsync(user);

            if (vm.Role == "Student")
            {
                await _repo.CreateStudentProfileAsync(new StudentProfile
                {
                    user_id = user.user_id,
                    profile_completion = 100
                });
            }
            else if (vm.Role == "Company")
            {
                await _repo.CreateCompanyProfileAsync(new CompanyProfile
                {
                    user_id = user.user_id,
                    company_name = vm.FullName
                });
            }

            return (true, uniqueKey, user);
        }

        // ── Login ─────────────────────────────────────────────────────
        public async Task<(bool, string, User?)> LoginAsync(LoginVM vm)
        {
            User? user = null;

            if (!string.IsNullOrEmpty(vm.LoginIdentifier) &&
                vm.LoginIdentifier.StartsWith("ASP-", StringComparison.OrdinalIgnoreCase))
            {
                user = await _repo.GetByUniqueKeyAsync(vm.LoginIdentifier.ToUpper());
            }
            else
            {
                user = await _repo.GetByEmailAsync(vm.LoginIdentifier);
            }

            if (user == null) return (false, "Invalid credentials. Please try again.", null);
            if (!user.is_active) return (false, "Account deactivated. Contact support.", null);
            if (!BCrypt.Net.BCrypt.Verify(vm.Password, user.hashed_password))
                return (false, "Invalid credentials. Please try again.", null);

            return (true, "Login successful.", user);
        }

        // ── Step 1: Education Level ───────────────────────────────────
        public async Task<(bool, string)> SaveStep1Async(int userId, OnboardingStep1VM vm)
        {
            var allowed = new[] { "Intermediate", "Undergraduate", "Graduate" };
            if (!allowed.Contains(vm.EducationLevel)) return (false, "Invalid education level.");

            var profile = await _repo.GetStudentProfileAsync(userId);
            if (profile == null) return (false, "Profile not found.");

            profile.education_level = vm.EducationLevel;
            profile.profile_completion = 20;

            await _repo.UpdateStudentProfileAsync(profile);
            return (true, "Step 1 saved.");
        }

        // ── Step 2: Academic Details ──────────────────────────────────
        // Ab UniversityId Universities table se validated hota hai —
        // koi bhi random text institute allow nahi (galat data enter nahi ho sakti)
        public async Task<(bool, string)> SaveStep2Async(int userId, OnboardingStep2VM vm)
        {
            var profile = await _repo.GetStudentProfileAsync(userId);
            if (profile == null) return (false, "Profile not found.");

            var university = await _db.Universities.FindAsync(vm.UniversityId);
            if (university == null)
                return (false, "Please select a valid university from the list.");

            profile.university_id = university.university_id;
            profile.university_name = university.name;
            profile.field_of_study = $"{vm.Program} - {vm.Major}";
            profile.profile_completion = 40;

            await _repo.UpdateStudentProfileAsync(profile);
            return (true, "Step 2 saved.");
        }

        // ── University list for Step2 dropdown ──────────────────────────
        public async Task<List<UniversityOption>> GetUniversitiesAsync()
        {
            return await _db.Universities
                .OrderBy(u => u.name)
                .Select(u => new UniversityOption
                {
                    UniversityId = u.university_id,
                    Name = u.name,
                    Location = u.location ?? ""
                })
                .ToListAsync();
        }

        // ── Step 3: Skills — MAIN FIX ───────────────────────────────────
        // Ab har skill DB ki Skills table se match honi zaroori hai (koi
        // bhi random skill jaise "cooking" reject ho jayegi), aur skills
        // ab StudentSkills table mein actually save bhi hoti hain
        // (pehle yahan sirf profile_completion update ho raha tha).
        public async Task<(bool, string)> SaveStep3Async(int userId, OnboardingStep3VM vm)
        {
            var profile = await _repo.GetStudentProfileAsync(userId);
            if (profile == null) return (false, "Profile not found.");

            if (vm.Skills == null || !vm.Skills.Any())
                return (false, "Please add at least one skill.");

            var allowedLevels = new[] { "Beginner", "Intermediate", "Advanced" };
            var toSave = new List<StudentSkill>();

            foreach (var item in vm.Skills)
            {
                if (!allowedLevels.Contains(item.SkillLevel))
                    return (false, $"Invalid skill level for '{item.SkillName}'.");

                var matched = await _skillCatalog.FindByNameAsync(item.SkillName);
                if (matched == null)
                    return (false, $"'{item.SkillName}' is not a recognized skill. Please choose from the suggested list.");

                toSave.Add(new StudentSkill
                {
                    skill_id = matched.skill_id,
                    proficiency_level = item.SkillLevel
                });
            }

            // 🔧 student_id (StudentProfile.student_id) bhejna zaroori hai, user_id nahi
            await _repo.SaveStudentSkillsAsync(profile.student_id, toSave);

            profile.profile_completion = 60;
            await _repo.UpdateStudentProfileAsync(profile);
            return (true, "Step 3 saved.");
        }

        // ── Step 4: Interests ─────────────────────────────────────────
        public async Task<(bool, string)> SaveStep4Async(int userId, OnboardingStep4VM vm)
        {
            var allowed = new[] { "Technology", "Business", "Design", "Medicine", "Engineering", "Arts" };
            if (vm.Interests.Any(i => !allowed.Contains(i)))
                return (false, "One or more interests are invalid.");

            var profile = await _repo.GetStudentProfileAsync(userId);
            if (profile == null) return (false, "Profile not found.");

            profile.interests = string.Join(",", vm.Interests);
            profile.profile_completion = 80;

            await _repo.UpdateStudentProfileAsync(profile);
            return (true, "Step 4 saved.");
        }

        // ── Step 5: Goals ─────────────────────────────────────────────
        public async Task<(bool, string)> SaveStep5Async(int userId, OnboardingStep5VM vm)
        {
            var allowed = new[] { "Get a Job", "Freelancing", "Higher Education", "Start a Business", "Just Learning" };
            if (!allowed.Contains(vm.Goal))
                return (false, "Invalid goal selected.");

            var profile = await _repo.GetStudentProfileAsync(userId);
            if (profile == null) return (false, "Profile not found.");

            profile.bio = $"Goal:{vm.Goal} | University:{profile.bio}";
            profile.profile_completion = 100;

            await _repo.UpdateStudentProfileAsync(profile);
            return (true, "Onboarding complete!");
        }

        // ── Forgot / Reset Password ─────────────────────────────────────
        public async Task<(bool, string)> ForgotPasswordAsync(string email)
        {
            var user = await _repo.GetByEmailAsync(email.ToLower().Trim());
            // Don't reveal whether the email exists — same message either way.
            if (user == null) return (true, "If that email is registered, an OTP has been sent.");

            var otp = _rng.Next(0, 1000000).ToString("D6");

            await _repo.CreatePasswordResetAsync(new PasswordReset
            {
                user_id = user.user_id,
                otp_code = otp,
                expires_at = DateTime.Now.AddMinutes(_otpExpiryMinutes),
                used = false,
                created_at = DateTime.Now
            });

            // TODO(optional): swap the plain-text body below for an HTML template.
            await _email.SendAsync(
                user.email,
                "Your Aspira Hub password reset OTP",
                $"Your OTP is {otp}. It expires in {_otpExpiryMinutes} minutes. " +
                "If you didn't request this, you can ignore this email."
            );

            return (true, "If that email is registered, an OTP has been sent.");
        }

        public async Task<(bool, string)> VerifyOtpAsync(string email, string otp)
        {
            var user = await _repo.GetByEmailAsync(email.ToLower().Trim());
            if (user == null) return (false, "Invalid or expired OTP.");

            var reset = await _repo.GetValidResetAsync(user.user_id, otp.Trim());
            if (reset == null) return (false, "Invalid or expired OTP.");

            return (true, "OTP verified.");
        }

        public async Task<(bool, string)> ResetPasswordAsync(string email, string otp, string newPassword)
        {
            var user = await _repo.GetByEmailAsync(email.ToLower().Trim());
            if (user == null) return (false, "Invalid or expired OTP.");

            var reset = await _repo.GetValidResetAsync(user.user_id, otp.Trim());
            if (reset == null) return (false, "Invalid or expired OTP.");

            user.hashed_password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _repo.UpdateAsync(user);
            await _repo.MarkResetUsedAsync(reset);

            return (true, "Password reset successful.");
        }
    }
}
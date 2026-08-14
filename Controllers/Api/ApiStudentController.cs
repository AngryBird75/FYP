using System.Linq;
using System.Security.Claims;
using AspiraHub.Data;
using AspiraHub.DTOs;
using AspiraHub.Models;
using AspiraHub.Repositories;
using AspiraHub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AspiraHub.Controllers.Api
{
    [ApiController]
    [Route("api/student")]
    [Authorize(Roles = "Student")]
    public class ApiStudentController : ControllerBase
    {
        private readonly IDashboardRepository _dashboard;
        private readonly IUserRepository _users;
        private readonly AppDbContext _db;
        private readonly ISkillCatalogService _skillCatalog;
        private readonly IWebHostEnvironment _env;

        private static readonly HashSet<string> AllowedCvExtensions = new(StringComparer.OrdinalIgnoreCase)
            { ".pdf", ".doc", ".docx" };
        private const long MaxCvSizeBytes = 5 * 1024 * 1024; // 5 MB

        public ApiStudentController(IDashboardRepository dashboard, IUserRepository users, AppDbContext db,
            ISkillCatalogService skillCatalog, IWebHostEnvironment env)
        {
            _dashboard = dashboard;
            _users = users;
            _db = db;
            _skillCatalog = skillCatalog;
            _env = env;
        }

        private int UserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // Everything the dashboard home screen needs in one call —
        // stats cards, progress, recent notifications, etc. (same VM
        // the website's StudentDashboard/Index view uses).
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var vm = await _dashboard.GetStudentDashboardAsync(UserId);

            // RecentNotifications / Announcements come back as the raw EF
            // models (Notification / Announcement), whose own property
            // names (notif_id, body vs content, is_read, ...) don't line up
            // with the Android app's NotificationDto/AnnouncementDto field
            // names. Project them here instead of touching the shared VM
            // (the website's StudentDashboard/Index view still uses vm as-is).
            var payload = new
            {
                vm.Name,
                vm.UniqueKey,
                vm.EducationLevel,
                vm.UniversityName,
                vm.Program,
                vm.Interests,
                vm.Goal,
                vm.ProfileCompletion,
                vm.ProfilePicture,
                vm.Skills,
                vm.CoursesInProgress,
                vm.CoursesCompleted,
                vm.TotalRoadmaps,
                vm.RoadmapProgress,
                vm.MatchedJobs,
                vm.AppliedJobs,
                vm.TopMatchedJobs,
                vm.UnreadNotifications,
                RecentNotifications = vm.RecentNotifications.Select(n => new
                {
                    id = n.notif_id,
                    message = n.body ?? n.title,
                    isRead = n.is_read,
                    createdAt = n.created_at
                }),
                Announcements = vm.Announcements.Select(a => new
                {
                    id = a.announcement_id,
                    title = a.title,
                    body = a.content
                })
            };

            return Ok(ApiResponse<object>.Ok(payload));
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var profile = await _users.GetStudentProfileAsync(UserId);
            if (profile == null) return NotFound(ApiResponse<object>.Fail("Profile not found"));
            return Ok(ApiResponse<object>.Ok(profile));
        }

        // Full edit-screen payload: profile fields + current skills + the
        // skill catalog suggested for the student's degree program (same
        // data the website's StudentDashboard/Profile edit view uses).
        [HttpGet("profile/edit")]
        public async Task<IActionResult> GetProfileForEdit()
        {
            var vm = await BuildProfileVMAsync();
            if (vm == null) return NotFound(ApiResponse<object>.Fail("Profile not found"));
            return Ok(ApiResponse<object>.Ok(vm));
        }

        public class UpdateProfileRequest
        {
            public string? Phone { get; set; }
            public string? City { get; set; }
            public string? Bio { get; set; }
            public string? FieldOfStudy { get; set; }
            public string? Interests { get; set; }
            public string? Goal { get; set; }
            public string? LinkedinUrl { get; set; }
            public string? ResumeUrl { get; set; }
            public int? CurrentSemester { get; set; }
        }

        // multipart/form-data: fields above as form values, plus an optional
        // cvFile. An uploaded file takes priority over ResumeUrl (a typed
        // link, e.g. Google Drive) — matches the website's behavior.
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromForm] UpdateProfileRequest form, IFormFile? cvFile)
        {
            var user = await _db.Users.Include(u => u.StudentProfile).FirstOrDefaultAsync(u => u.user_id == UserId);
            if (user?.StudentProfile == null) return NotFound(ApiResponse<object>.Fail("Profile not found"));

            var p = user.StudentProfile;
            p.phone = form.Phone;
            p.city = form.City;
            p.bio = form.Bio;
            p.field_of_study = form.FieldOfStudy;
            p.interests = form.Interests;
            p.goal = form.Goal;
            p.linkedin_url = form.LinkedinUrl;

            var errors = new List<string>();

            // Current semester is only meaningful for Undergraduate/Graduate students.
            if (p.education_level == "Undergraduate" || p.education_level == "Graduate")
            {
                int totalSemesters = 8;
                if (p.degree_program_id.HasValue)
                {
                    var dp = await _db.DegreePrograms.FindAsync(p.degree_program_id.Value);
                    if (dp != null) totalSemesters = dp.total_semesters;
                }

                if (form.CurrentSemester.HasValue)
                {
                    if (form.CurrentSemester.Value < 1 || form.CurrentSemester.Value > totalSemesters)
                        errors.Add($"Current semester must be between 1 and {totalSemesters}.");
                    else
                        p.current_semester = form.CurrentSemester.Value;
                }
            }

            if (cvFile != null && cvFile.Length > 0)
            {
                var ext = Path.GetExtension(cvFile.FileName);
                if (!AllowedCvExtensions.Contains(ext))
                {
                    errors.Add("CV must be a PDF, DOC, or DOCX file.");
                }
                else if (cvFile.Length > MaxCvSizeBytes)
                {
                    errors.Add("CV file is too large (max 5 MB).");
                }
                else
                {
                    var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "resumes");
                    Directory.CreateDirectory(uploadsDir);

                    var fileName = $"student_{p.student_id}_{Guid.NewGuid():N}{ext}";
                    var fullPath = Path.Combine(uploadsDir, fileName);

                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await cvFile.CopyToAsync(stream);
                    }

                    p.resume_url = $"/uploads/resumes/{fileName}";
                }
            }
            else
            {
                p.resume_url = form.ResumeUrl;
            }

            if (errors.Count > 0)
                return BadRequest(ApiResponse<object>.Fail(string.Join(" ", errors)));

            // Recompute a simple profile completion score
            var fields = new[] { p.phone, p.city, p.bio, p.education_level, p.program, p.university_name,
                                  p.field_of_study, p.interests, p.goal, p.resume_url, p.linkedin_url };
            int filled = fields.Count(f => !string.IsNullOrWhiteSpace(f));
            bool hasSkills = await _db.StudentSkills.AnyAsync(s => s.student_id == p.student_id);
            p.profile_completion = (int)Math.Round((filled + (hasSkills ? 1 : 0)) * 100.0 / (fields.Length + 1));

            await _db.SaveChangesAsync();

            return Ok(ApiResponse<string>.Ok("", "Profile updated successfully"));
        }

        public class AddSkillRequest { public string SkillName { get; set; } = ""; public string SkillLevel { get; set; } = ""; }

        [HttpPost("skills")]
        public async Task<IActionResult> AddSkill(AddSkillRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.SkillName))
                return BadRequest(ApiResponse<object>.Fail("Skill name is required."));

            var allowedLevels = new[] { "Beginner", "Intermediate", "Advanced" };
            if (!allowedLevels.Contains(req.SkillLevel))
                return BadRequest(ApiResponse<object>.Fail("Invalid skill level."));

            var studentId = await GetStudentIdAsync();
            if (studentId == 0) return NotFound(ApiResponse<object>.Fail("Profile not found."));

            var skill = await _skillCatalog.FindByNameAsync(req.SkillName);
            if (skill == null)
                return BadRequest(ApiResponse<object>.Fail($"'{req.SkillName}' is not a recognized skill."));

            bool exists = await _db.StudentSkills.AnyAsync(ss => ss.student_id == studentId && ss.skill_id == skill.skill_id);
            if (exists)
                return BadRequest(ApiResponse<object>.Fail("You already have this skill."));

            _db.StudentSkills.Add(new StudentSkill
            {
                student_id = studentId,
                skill_id = skill.skill_id,
                proficiency_level = req.SkillLevel,
                category = skill.category
            });
            await _db.SaveChangesAsync();

            return Ok(ApiResponse<object>.Ok(new { skillId = skill.skill_id, skillName = skill.skill_name, level = req.SkillLevel }, "Added"));
        }

        [HttpDelete("skills/{skillId:int}")]
        public async Task<IActionResult> RemoveSkill(int skillId)
        {
            var studentId = await GetStudentIdAsync();
            if (studentId == 0) return NotFound(ApiResponse<object>.Fail("Profile not found."));

            var row = await _db.StudentSkills.FirstOrDefaultAsync(ss => ss.student_id == studentId && ss.skill_id == skillId);
            if (row == null) return NotFound(ApiResponse<object>.Fail("Skill not found."));

            _db.StudentSkills.Remove(row);
            await _db.SaveChangesAsync();

            return Ok(ApiResponse<string>.Ok("", "Removed"));
        }

        // ══════════════════════════════════════════════
        private async Task<object?> BuildProfileVMAsync()
        {
            var user = await _db.Users.Include(u => u.StudentProfile).FirstOrDefaultAsync(u => u.user_id == UserId);
            if (user?.StudentProfile == null) return null;

            var p = user.StudentProfile;

            var skills = await (
                from ss in _db.StudentSkills
                join sk in _db.Skills on ss.skill_id equals sk.skill_id
                where ss.student_id == p.student_id
                select new { skillId = sk.skill_id, skillName = sk.skill_name, proficiencyLevel = ss.proficiency_level }
            ).ToListAsync();

            var catalog = await _skillCatalog.GetSkillNamesForProgramAsync(p.degree_program_id);

            int? totalSemesters = null;
            if (p.degree_program_id.HasValue)
            {
                totalSemesters = await _db.DegreePrograms
                    .Where(dp => dp.program_id == p.degree_program_id.Value)
                    .Select(dp => (int?)dp.total_semesters)
                    .FirstOrDefaultAsync();
            }

            return new
            {
                name = user.name,
                email = user.email,
                phone = p.phone,
                city = p.city,
                bio = p.bio,
                educationLevel = p.education_level ?? "",
                program = p.program,
                universityName = p.university_name,
                fieldOfStudy = p.field_of_study,
                interests = p.interests,
                goal = p.goal,
                resumeUrl = p.resume_url,
                linkedinUrl = p.linkedin_url,
                currentSemester = p.current_semester,
                totalSemesters = totalSemesters ?? 8,
                profileCompletion = p.profile_completion,
                uniqueKey = user.unique_key,
                skills,
                skillCatalog = catalog
            };
        }

        private async Task<int> GetStudentIdAsync()
        {
            var profile = await _users.GetStudentProfileAsync(UserId);
            return profile?.student_id ?? 0;
        }
    }
}

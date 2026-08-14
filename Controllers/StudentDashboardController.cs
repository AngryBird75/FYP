using AspiraHub.Data;
using AspiraHub.Repositories;
using AspiraHub.Services;
using AspiraHub.ViewModels.Dashboard;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AspiraHub.Controllers
{
    public class StudentDashboardController : Controller
    {
        private readonly IDashboardRepository _repo;
        private readonly AppDbContext _db;
        private readonly ISkillCatalogService _skillCatalog;
        private readonly IWebHostEnvironment _env;

        private static readonly HashSet<string> AllowedCvExtensions = new(StringComparer.OrdinalIgnoreCase)
            { ".pdf", ".doc", ".docx" };
        private const long MaxCvSizeBytes = 5 * 1024 * 1024; // 5 MB

        public StudentDashboardController(IDashboardRepository repo, AppDbContext db, ISkillCatalogService skillCatalog, IWebHostEnvironment env)
        {
            _repo = repo;
            _db = db;
            _skillCatalog = skillCatalog;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            if (!IsStudent()) return RedirectToAction("Login", "Auth");

            var vm = await _repo.GetStudentDashboardAsync(GetUserId());
            return View(vm);
        }

        public class IdRequest { public int Id { get; set; } }

        [HttpPost]
        public async Task<IActionResult> MarkNotificationRead([FromBody] IdRequest req)
        {
            if (!IsStudent()) return Unauthorized();
            if (req == null) return Json(new { success = false });

            await _repo.MarkNotificationReadAsync(req.Id, GetUserId());
            return Json(new { success = true });
        }

        // ══════════════════════════════════════════════
        // PROFILE / SETTINGS
        // ══════════════════════════════════════════════
        public async Task<IActionResult> Profile()
        {
            if (!IsStudent()) return RedirectToAction("Login", "Auth");

            var vm = await BuildProfileVMAsync();
            if (vm == null) return RedirectToAction("Index");

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Profile(StudentProfileEditVM form, IFormFile? cvFile)
        {
            if (!IsStudent()) return RedirectToAction("Login", "Auth");

            var user = await _db.Users.Include(u => u.StudentProfile).FirstOrDefaultAsync(u => u.user_id == GetUserId());
            if (user?.StudentProfile == null) return RedirectToAction("Index");

            var p = user.StudentProfile;
            p.phone = form.Phone;
            p.city = form.City;
            p.bio = form.Bio;
            p.field_of_study = form.FieldOfStudy;
            p.interests = form.Interests;
            p.goal = form.Goal;
            p.linkedin_url = form.LinkedinUrl;

            // Current semester is only meaningful for Undergraduate/Graduate students
            // (Intermediate students don't have this field on their onboarding path).
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
                        ModelState.AddModelError("", $"Current semester must be between 1 and {totalSemesters}.");
                    else
                        p.current_semester = form.CurrentSemester.Value;
                }
            }

            // CV attachment: an actually-uploaded file takes priority. If the
            // student didn't upload a new file, fall back to whatever they typed
            // in the Resume URL box (e.g. a Google Drive / Dropbox link) so both
            // ways of attaching a CV keep working.
            if (cvFile != null && cvFile.Length > 0)
            {
                var ext = Path.GetExtension(cvFile.FileName);
                if (!AllowedCvExtensions.Contains(ext))
                {
                    ModelState.AddModelError("", "CV must be a PDF, DOC, or DOCX file.");
                }
                else if (cvFile.Length > MaxCvSizeBytes)
                {
                    ModelState.AddModelError("", "CV file is too large (max 5 MB).");
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

            if (!ModelState.IsValid)
            {
                var vm = await BuildProfileVMAsync();
                if (vm != null)
                {
                    vm.Phone = form.Phone; vm.City = form.City; vm.Bio = form.Bio;
                    vm.FieldOfStudy = form.FieldOfStudy; vm.Interests = form.Interests; vm.Goal = form.Goal;
                    vm.LinkedinUrl = form.LinkedinUrl; vm.ResumeUrl = form.ResumeUrl;
                    vm.CurrentSemester = form.CurrentSemester;
                }
                TempData["ProfileError"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return View(vm);
            }

            // Recompute a simple profile completion score
            var fields = new[] { p.phone, p.city, p.bio, p.education_level, p.program, p.university_name,
                                  p.field_of_study, p.interests, p.goal, p.resume_url, p.linkedin_url };
            int filled = fields.Count(f => !string.IsNullOrWhiteSpace(f));
            bool hasSkills = await _db.StudentSkills.AnyAsync(s => s.student_id == p.student_id);
            p.profile_completion = (int)Math.Round((filled + (hasSkills ? 1 : 0)) * 100.0 / (fields.Length + 1));

            await _db.SaveChangesAsync();

            TempData["ProfileSaved"] = "Profile updated successfully.";
            return RedirectToAction("Profile");
        }

        public class AddSkillRequest { public string SkillName { get; set; } = ""; public string SkillLevel { get; set; } = ""; }

        [HttpPost]
        public async Task<IActionResult> AddSkill([FromBody] AddSkillRequest req)
        {
            if (!IsStudent()) return Unauthorized();
            if (req == null || string.IsNullOrWhiteSpace(req.SkillName))
                return Json(new { success = false, message = "Skill name is required." });

            var allowedLevels = new[] { "Beginner", "Intermediate", "Advanced" };
            if (!allowedLevels.Contains(req.SkillLevel))
                return Json(new { success = false, message = "Invalid skill level." });

            var studentId = await GetStudentIdAsync();
            if (studentId == 0) return Json(new { success = false, message = "Profile not found." });

            var skill = await _skillCatalog.FindByNameAsync(req.SkillName);
            if (skill == null)
                return Json(new { success = false, message = $"'{req.SkillName}' is not a recognized skill." });

            bool exists = await _db.StudentSkills.AnyAsync(ss => ss.student_id == studentId && ss.skill_id == skill.skill_id);
            if (exists)
                return Json(new { success = false, message = "You already have this skill." });

            _db.StudentSkills.Add(new Models.StudentSkill
            {
                student_id = studentId,
                skill_id = skill.skill_id,
                proficiency_level = req.SkillLevel,
                category = skill.category
            });
            await _db.SaveChangesAsync();

            return Json(new { success = true, skillId = skill.skill_id, skillName = skill.skill_name, level = req.SkillLevel });
        }

        public class SkillIdRequest { public int SkillId { get; set; } }

        [HttpPost]
        public async Task<IActionResult> RemoveSkill([FromBody] SkillIdRequest req)
        {
            if (!IsStudent()) return Unauthorized();
            if (req == null) return Json(new { success = false });

            var studentId = await GetStudentIdAsync();
            if (studentId == 0) return Json(new { success = false, message = "Profile not found." });

            var row = await _db.StudentSkills.FirstOrDefaultAsync(ss => ss.student_id == studentId && ss.skill_id == req.SkillId);
            if (row == null) return Json(new { success = false, message = "Skill not found." });

            _db.StudentSkills.Remove(row);
            await _db.SaveChangesAsync();

            return Json(new { success = true });
        }

        // ══════════════════════════════════════════════
        private async Task<StudentProfileEditVM?> BuildProfileVMAsync()
        {
            var user = await _db.Users.Include(u => u.StudentProfile).FirstOrDefaultAsync(u => u.user_id == GetUserId());
            if (user?.StudentProfile == null) return null;

            var p = user.StudentProfile;

            var skills = await (
                from ss in _db.StudentSkills
                join sk in _db.Skills on ss.skill_id equals sk.skill_id
                where ss.student_id == p.student_id
                select new StudentSkillRowVM { SkillId = sk.skill_id, SkillName = sk.skill_name, ProficiencyLevel = ss.proficiency_level }
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

            return new StudentProfileEditVM
            {
                Name = user.name,
                Email = user.email,
                Phone = p.phone,
                City = p.city,
                Bio = p.bio,
                EducationLevel = p.education_level ?? "",
                Program = p.program,
                UniversityName = p.university_name,
                FieldOfStudy = p.field_of_study,
                Interests = p.interests,
                Goal = p.goal,
                ResumeUrl = p.resume_url,
                LinkedinUrl = p.linkedin_url,
                CurrentSemester = p.current_semester,
                TotalSemesters = totalSemesters ?? 8,
                ProfileCompletion = p.profile_completion,
                UniqueKey = user.unique_key,
                Skills = skills,
                SkillCatalog = catalog
            };
        }

        private bool IsStudent()
            => HttpContext.Session.GetString("Role") == "Student";

        private int GetUserId()
            => HttpContext.Session.GetInt32("UserId") ?? 0;

        private async Task<int> GetStudentIdAsync()
        {
            int userId = GetUserId();
            if (userId == 0) return 0;
            return await _db.StudentProfiles.Where(s => s.user_id == userId).Select(s => s.student_id).FirstOrDefaultAsync();
        }
    }
}

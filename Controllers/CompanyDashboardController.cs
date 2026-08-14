using AspiraHub.Data;
using AspiraHub.Models;
using AspiraHub.Repositories;
using AspiraHub.ViewModels.Dashboard;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AspiraHub.Controllers
{
    public class CompanyDashboardController : Controller
    {
        private readonly IDashboardRepository _repo;
        private readonly AppDbContext _db;

        public CompanyDashboardController(IDashboardRepository repo, AppDbContext db)
        {
            _repo = repo;
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            if (!IsCompany()) return RedirectToAction("Login", "Auth");

            var vm = await _repo.GetCompanyDashboardAsync(GetUserId());
            return View(vm);
        }

        public class IdRequest { public int Id { get; set; } }

        [HttpPost]
        public async Task<IActionResult> MarkNotificationRead([FromBody] IdRequest req)
        {
            if (!IsCompany()) return Unauthorized();
            if (req == null) return Json(new { success = false });

            await _repo.MarkNotificationReadAsync(req.Id, GetUserId());
            return Json(new { success = true });
        }

        // ══════════════════════════════════════════════
        // MY JOB POSTS
        // ══════════════════════════════════════════════
        public async Task<IActionResult> MyJobs(string? search, string? status, int page = 1)
        {
            if (!IsCompany()) return RedirectToAction("Login", "Auth");

            var company = await GetMyCompanyAsync();
            if (company == null) return RedirectToAction("Index");

            const int pageSize = 10;
            var query = _db.JobPostings.Where(j => j.company_id == company.company_id).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(j => j.title.ToLower().Contains(s));
            }
            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                query = query.Where(j => j.status == status);
            }

            int totalCount = await query.CountAsync();

            var jobs = await query
                .OrderByDescending(j => j.posted_date)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(j => new CompanyJobVM
                {
                    JobId = j.job_id,
                    Title = j.title,
                    Location = j.location ?? "",
                    JobType = j.job_type ?? "",
                    Status = j.status,
                    Views = j.views_count,
                    Applications = j.applications_count,
                    Deadline = j.deadline,
                    PostedDate = j.posted_date
                })
                .ToListAsync();

            var vm = new CompanyJobsListVM
            {
                Jobs = jobs,
                Search = search ?? "",
                StatusFilter = status ?? "All",
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                IsVerified = company.is_verified
            };

            return View(vm);
        }

        public class JobIdRequest { public int JobId { get; set; } }

        [HttpPost]
        public async Task<IActionResult> ToggleJobStatus([FromBody] JobIdRequest req)
        {
            if (!IsCompany()) return Unauthorized();
            if (req == null) return Json(new { success = false, message = "Invalid request" });

            var company = await GetMyCompanyAsync();
            if (company == null) return Json(new { success = false, message = "Company profile not found" });

            var job = await _db.JobPostings.FirstOrDefaultAsync(j => j.job_id == req.JobId && j.company_id == company.company_id);
            if (job == null) return Json(new { success = false, message = "Job not found" });

            job.status = job.status == "Active" ? "Closed" : "Active";
            await _db.SaveChangesAsync();

            return Json(new { success = true, status = job.status });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteJob([FromBody] JobIdRequest req)
        {
            if (!IsCompany()) return Unauthorized();
            if (req == null) return Json(new { success = false, message = "Invalid request" });

            var company = await GetMyCompanyAsync();
            if (company == null) return Json(new { success = false, message = "Company profile not found" });

            var job = await _db.JobPostings.FirstOrDefaultAsync(j => j.job_id == req.JobId && j.company_id == company.company_id);
            if (job == null) return Json(new { success = false, message = "Job not found" });

            try
            {
                _db.JobApplications.RemoveRange(_db.JobApplications.Where(a => a.job_id == req.JobId));
                _db.JobSkills.RemoveRange(_db.JobSkills.Where(js => js.job_id == req.JobId));
                _db.JobMatchings.RemoveRange(_db.JobMatchings.Where(m => m.job_id == req.JobId));
                _db.JobPostings.Remove(job);
                await _db.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Could not delete this job posting." });
            }
        }

        // ══════════════════════════════════════════════
        // POST A JOB (create) / EDIT JOB
        // ══════════════════════════════════════════════
        public async Task<IActionResult> PostJob(int? id)
        {
            if (!IsCompany()) return RedirectToAction("Login", "Auth");

            var company = await GetMyCompanyAsync();
            if (company == null) return RedirectToAction("Index");

            var vm = new JobPostFormVM { IsVerified = company.is_verified };

            if (id.HasValue)
            {
                var job = await _db.JobPostings
                    .Include(j => j.JobSkills)
                    .FirstOrDefaultAsync(j => j.job_id == id.Value && j.company_id == company.company_id);
                if (job == null) return NotFound();

                vm.JobId = job.job_id;
                vm.Title = job.title;
                vm.Description = job.description;
                vm.Location = job.location;
                vm.IndustryType = job.industry_type;
                vm.JobType = job.job_type;
                vm.Salary = job.salary;
                vm.JobTime = job.job_time;
                vm.Experience = job.experience;
                vm.ContactEmail = job.contact_email;
                vm.Website = job.website;
                vm.Deadline = job.deadline;
                vm.SelectedSkillIds = job.JobSkills.Select(js => js.skill_id).ToList();
            }

            vm.AllSkills = await _db.Skills
                .OrderBy(s => s.category).ThenBy(s => s.skill_name)
                .Select(s => new SkillOptionVM
                {
                    SkillId = s.skill_id,
                    Name = s.skill_name,
                    Category = s.category,
                    Selected = vm.SelectedSkillIds.Contains(s.skill_id)
                })
                .ToListAsync();

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> PostJob(JobPostFormVM vm)
        {
            if (!IsCompany()) return RedirectToAction("Login", "Auth");

            var company = await GetMyCompanyAsync();
            if (company == null) return RedirectToAction("Index");

            if (string.IsNullOrWhiteSpace(vm.Title))
            {
                ModelState.AddModelError("Title", "Job title is required.");
            }

            if (!ModelState.IsValid)
            {
                vm.IsVerified = company.is_verified;
                vm.AllSkills = await _db.Skills
                    .OrderBy(s => s.category).ThenBy(s => s.skill_name)
                    .Select(s => new SkillOptionVM
                    {
                        SkillId = s.skill_id,
                        Name = s.skill_name,
                        Category = s.category,
                        Selected = vm.SelectedSkillIds.Contains(s.skill_id)
                    })
                    .ToListAsync();
                return View(vm);
            }

            JobPosting job;
            if (vm.JobId.HasValue)
            {
                job = await _db.JobPostings
                    .Include(j => j.JobSkills)
                    .FirstOrDefaultAsync(j => j.job_id == vm.JobId.Value && j.company_id == company.company_id);
                if (job == null) return NotFound();

                _db.JobSkills.RemoveRange(job.JobSkills);
            }
            else
            {
                job = new JobPosting { company_id = company.company_id, posted_date = DateTime.Now, status = "Active" };
                _db.JobPostings.Add(job);
            }

            job.title = vm.Title.Trim();
            job.description = vm.Description;
            job.location = vm.Location;
            job.industry_type = vm.IndustryType;
            job.job_type = vm.JobType;
            job.salary = vm.Salary;
            job.job_time = vm.JobTime;
            job.experience = vm.Experience;
            job.contact_email = vm.ContactEmail;
            job.website = vm.Website;
            job.deadline = vm.Deadline;

            await _db.SaveChangesAsync();

            foreach (var skillId in vm.SelectedSkillIds.Distinct())
            {
                _db.JobSkills.Add(new JobSkill { job_id = job.job_id, skill_id = skillId });
            }
            await _db.SaveChangesAsync();

            TempData["JobSaved"] = vm.JobId.HasValue ? "Job updated successfully." : "Job posted successfully.";
            return RedirectToAction("MyJobs");
        }

        // ══════════════════════════════════════════════
        // APPLICATIONS
        // ══════════════════════════════════════════════
        public async Task<IActionResult> Applications(int? jobId, string? status, int page = 1)
        {
            if (!IsCompany()) return RedirectToAction("Login", "Auth");

            var company = await GetMyCompanyAsync();
            if (company == null) return RedirectToAction("Index");

            const int pageSize = 15;
            var myJobIds = _db.JobPostings.Where(j => j.company_id == company.company_id).Select(j => j.job_id);

            var query = _db.JobApplications.Where(a => myJobIds.Contains(a.job_id)).AsQueryable();

            if (jobId.HasValue) query = query.Where(a => a.job_id == jobId.Value);
            if (!string.IsNullOrWhiteSpace(status) && status != "All") query = query.Where(a => a.status == status);

            int totalCount = await query.CountAsync();

            var applications = await query
                .Include(a => a.Student).ThenInclude(s => s.User)
                .Include(a => a.JobPosting)
                .OrderByDescending(a => a.applied_at)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new CompanyApplicationRowVM
                {
                    ApplicationId = a.application_id,
                    JobId = a.job_id,
                    JobTitle = a.JobPosting.title,
                    StudentName = a.Student.User.name,
                    StudentEmail = a.Student.User.email,
                    Status = a.status,
                    ResumeUrl = a.resume_url,
                    CoverLetter = a.cover_letter,
                    AppliedAt = a.applied_at
                })
                .ToListAsync();

            var jobOptions = await _db.JobPostings
                .Where(j => j.company_id == company.company_id)
                .OrderByDescending(j => j.posted_date)
                .Select(j => new CompanyJobFilterOptionVM { JobId = j.job_id, Title = j.title })
                .ToListAsync();

            var vm = new CompanyApplicationsListVM
            {
                Applications = applications,
                JobOptions = jobOptions,
                JobIdFilter = jobId,
                StatusFilter = status ?? "All",
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return View(vm);
        }

        // ══════════════════════════════════════════════
        // VIEW APPLICANT PROFILE (read-only, scoped to companies own job postings)
        // ══════════════════════════════════════════════
        public async Task<IActionResult> ApplicantProfile(int applicationId)
        {
            if (!IsCompany()) return RedirectToAction("Login", "Auth");

            var company = await GetMyCompanyAsync();
            if (company == null) return RedirectToAction("Index");

            // A company may only view the profile of a student who applied to
            // one of ITS OWN job postings — never any student in the system.
            var myJobIds = _db.JobPostings.Where(j => j.company_id == company.company_id).Select(j => j.job_id);

            var app = await _db.JobApplications
                .Include(a => a.JobPosting)
                .Include(a => a.Student).ThenInclude(s => s.User)
                .FirstOrDefaultAsync(a => a.application_id == applicationId && myJobIds.Contains(a.job_id));

            if (app == null) return NotFound();

            var skills = await (
                from ss in _db.StudentSkills
                join sk in _db.Skills on ss.skill_id equals sk.skill_id
                where ss.student_id == app.student_id
                select new StudentSkillRowVM { SkillId = sk.skill_id, SkillName = sk.skill_name, ProficiencyLevel = ss.proficiency_level }
            ).ToListAsync();

            var p = app.Student;
            var vm = new CompanyApplicantProfileVM
            {
                ApplicationId = app.application_id,
                JobTitle = app.JobPosting.title,
                Status = app.status,
                CoverLetter = app.cover_letter,
                // Prefer the CV that was attached at application time; fall back to
                // whatever is currently on the student's profile.
                ResumeUrl = !string.IsNullOrWhiteSpace(app.resume_url) ? app.resume_url : p.resume_url,
                AppliedAt = app.applied_at,

                Name = p.User.name,
                Email = p.User.email,
                Phone = p.phone,
                City = p.city,
                Bio = p.bio,
                EducationLevel = p.education_level,
                Program = p.program,
                UniversityName = p.university_name,
                FieldOfStudy = p.field_of_study,
                Interests = p.interests,
                Goal = p.goal,
                LinkedinUrl = p.linkedin_url,
                Skills = skills
            };

            return View(vm);
        }

        public class UpdateAppStatusRequest { public int ApplicationId { get; set; } public string Status { get; set; } = ""; }

        [HttpPost]
        public async Task<IActionResult> UpdateApplicationStatus([FromBody] UpdateAppStatusRequest req)
        {
            if (!IsCompany()) return Unauthorized();
            if (req == null || req.Status is not ("Pending" or "Reviewed" or "Rejected"))
                return Json(new { success = false, message = "Invalid status" });

            var company = await GetMyCompanyAsync();
            if (company == null) return Json(new { success = false, message = "Company profile not found" });

            var myJobIds = _db.JobPostings.Where(j => j.company_id == company.company_id).Select(j => j.job_id);
            var app = await _db.JobApplications.FirstOrDefaultAsync(a => a.application_id == req.ApplicationId && myJobIds.Contains(a.job_id));
            if (app == null) return Json(new { success = false, message = "Application not found" });

            app.status = req.Status;
            app.reviewed_at = DateTime.Now;
            await _db.SaveChangesAsync();

            // Notify the student. This is best-effort: the status change above has
            // already been saved, so a problem creating the notification (e.g. a
            // database CHECK constraint on Notifications.type rejecting this value)
            // must NOT fail the status update / Reject action itself.
            try
            {
                var studentUserId = await _db.StudentProfiles.Where(s => s.student_id == app.student_id).Select(s => s.user_id).FirstOrDefaultAsync();
                if (studentUserId != 0)
                {
                    _db.Notifications.Add(new Notification
                    {
                        user_id = studentUserId,
                        type = "application",
                        title = "Application Status Updated",
                        body = $"Your application status changed to '{req.Status}'.",
                        ref_id = app.job_id,
                        created_at = DateTime.Now
                    });
                    await _db.SaveChangesAsync();
                }
            }
            catch (Exception)
            {
                // Swallow: notification is a nice-to-have, not critical to the
                // reject/approve/review action. See CK_Notifications_Type note.
            }

            return Json(new { success = true, status = app.status });
        }

        // ══════════════════════════════════════════════
        // COMPANY PROFILE
        // ══════════════════════════════════════════════
        public async Task<IActionResult> Profile()
        {
            if (!IsCompany()) return RedirectToAction("Login", "Auth");

            var user = await _db.Users.Include(u => u.CompanyProfile).FirstOrDefaultAsync(u => u.user_id == GetUserId());
            if (user?.CompanyProfile == null) return RedirectToAction("Index");

            var c = user.CompanyProfile;
            var vm = new CompanyProfileEditVM
            {
                CompanyName = c.company_name,
                Email = user.email,
                Industry = c.industry,
                Location = c.location,
                Description = c.description,
                Website = c.website,
                LogoUrl = c.logo_url,
                EmployeeCount = c.employee_count,
                FoundedYear = c.founded_year,
                IsVerified = c.is_verified,
                CreatedAt = user.created_at,
                UniqueKey = user.unique_key
            };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Profile(CompanyProfileEditVM vm)
        {
            if (!IsCompany()) return RedirectToAction("Login", "Auth");

            if (string.IsNullOrWhiteSpace(vm.CompanyName))
                ModelState.AddModelError("CompanyName", "Company name is required.");

            var user = await _db.Users.Include(u => u.CompanyProfile).FirstOrDefaultAsync(u => u.user_id == GetUserId());
            if (user?.CompanyProfile == null) return RedirectToAction("Index");

            if (!ModelState.IsValid)
            {
                vm.Email = user.email;
                vm.IsVerified = user.CompanyProfile.is_verified;
                vm.CreatedAt = user.created_at;
                vm.UniqueKey = user.unique_key;
                return View(vm);
            }

            var c = user.CompanyProfile;
            c.company_name = vm.CompanyName.Trim();
            c.industry = vm.Industry;
            c.location = vm.Location;
            c.description = vm.Description;
            c.website = vm.Website;
            c.logo_url = vm.LogoUrl;
            c.employee_count = vm.EmployeeCount;
            c.founded_year = vm.FoundedYear;
            // is_verified is intentionally NOT editable here — only Admin can verify a company

            await _db.SaveChangesAsync();

            TempData["ProfileSaved"] = "Company profile updated successfully.";
            return RedirectToAction("Profile");
        }

        // ══════════════════════════════════════════════
        // NOTIFICATIONS
        // ══════════════════════════════════════════════
        public async Task<IActionResult> Notifications()
        {
            if (!IsCompany()) return RedirectToAction("Login", "Auth");

            var userId = GetUserId();
            var notifs = await _db.Notifications
                .Where(n => n.user_id == userId)
                .OrderByDescending(n => n.created_at)
                .Take(50)
                .ToListAsync();

            var vm = new CompanyNotificationsVM
            {
                Notifications = notifs,
                UnreadCount = notifs.Count(n => !n.is_read)
            };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllNotificationsRead()
        {
            if (!IsCompany()) return Unauthorized();

            var userId = GetUserId();
            var unread = await _db.Notifications.Where(n => n.user_id == userId && !n.is_read).ToListAsync();
            foreach (var n in unread) n.is_read = true;
            await _db.SaveChangesAsync();

            return Json(new { success = true });
        }

        // ══════════════════════════════════════════════
        private async Task<CompanyProfile?> GetMyCompanyAsync()
            => await _db.CompanyProfiles.FirstOrDefaultAsync(c => c.user_id == GetUserId());

        private bool IsCompany()
            => HttpContext.Session.GetString("Role") == "Company";

        private int GetUserId()
            => HttpContext.Session.GetInt32("UserId") ?? 0;
    }
}

using System.Security.Claims;
using AspiraHub.Data;
using AspiraHub.DTOs;
using AspiraHub.Models;
using AspiraHub.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AspiraHub.Controllers.Api
{
    // Mobile-facing mirror of CompanyDashboardController — same
    // IDashboardRepository / AppDbContext the website's Company
    // dashboard, MyJobs, PostJob, Applications and Profile pages use,
    // just returning JSON instead of Views. Notifications for Company
    // are already covered by ApiNotificationController (role-agnostic).
    [ApiController]
    [Route("api/company")]
    [Authorize(Roles = "Company")]
    public class ApiCompanyController : ControllerBase
    {
        private readonly IDashboardRepository _repo;
        private readonly AppDbContext _db;

        public ApiCompanyController(IDashboardRepository repo, AppDbContext db)
        {
            _repo = repo;
            _db = db;
        }

        private int UserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        private async Task<CompanyProfile?> GetMyCompanyAsync()
            => await _db.CompanyProfiles.FirstOrDefaultAsync(c => c.user_id == UserId);

        // ══════════════════════════════════════════════
        // DASHBOARD
        // ══════════════════════════════════════════════
        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            var vm = await _repo.GetCompanyDashboardAsync(UserId);
            return Ok(ApiResponse<object>.Ok(vm));
        }

        // ══════════════════════════════════════════════
        // SKILL CATALOG (for the mobile "Post a Job" screen's skill
        // picker — the website's PostJob.cshtml gets its skill checkboxes
        // from a ViewBag populated server-side by the MVC action; the
        // JSON API needs its own explicit route for the same data.)
        // ══════════════════════════════════════════════
        [HttpGet("skills")]
        public async Task<IActionResult> Skills()
        {
            var skills = await _db.Skills
                .OrderBy(s => s.skill_name)
                .Select(s => new { skillId = s.skill_id, skillName = s.skill_name })
                .ToListAsync();

            return Ok(ApiResponse<object>.Ok(skills));
        }

        // ══════════════════════════════════════════════
        // MY JOB POSTS
        // ══════════════════════════════════════════════
        [HttpGet("jobs")]
        public async Task<IActionResult> MyJobs([FromQuery] string? search, [FromQuery] string? status, [FromQuery] int page = 1)
        {
            var company = await GetMyCompanyAsync();
            if (company == null) return NotFound(ApiResponse<object>.Fail("Company profile not found"));

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
                .Select(j => new
                {
                    jobId = j.job_id,
                    title = j.title,
                    location = j.location ?? "",
                    jobType = j.job_type ?? "",
                    status = j.status,
                    views = j.views_count,
                    applications = j.applications_count,
                    deadline = j.deadline,
                    postedDate = j.posted_date
                })
                .ToListAsync();

            return Ok(ApiResponse<object>.Ok(new
            {
                jobs,
                search = search ?? "",
                statusFilter = status ?? "All",
                page,
                pageSize,
                totalCount,
                isVerified = company.is_verified
            }));
        }

        // Single job, for pre-filling the mobile "edit job" screen.
        [HttpGet("jobs/{jobId:int}")]
        public async Task<IActionResult> GetJob(int jobId)
        {
            var company = await GetMyCompanyAsync();
            if (company == null) return NotFound(ApiResponse<object>.Fail("Company profile not found"));

            var job = await _db.JobPostings
                .Include(j => j.JobSkills)
                .FirstOrDefaultAsync(j => j.job_id == jobId && j.company_id == company.company_id);
            if (job == null) return NotFound(ApiResponse<object>.Fail("Job not found"));

            return Ok(ApiResponse<object>.Ok(new
            {
                jobId = job.job_id,
                title = job.title,
                description = job.description,
                location = job.location,
                industryType = job.industry_type,
                jobType = job.job_type,
                salary = job.salary,
                jobTime = job.job_time,
                experience = job.experience,
                contactEmail = job.contact_email,
                website = job.website,
                deadline = job.deadline,
                skillIds = job.JobSkills.Select(js => js.skill_id).ToList()
            }));
        }

        public class JobPostRequest
        {
            public string Title { get; set; } = "";
            public string? Description { get; set; }
            public string? Location { get; set; }
            public string? IndustryType { get; set; }
            public string? JobType { get; set; }
            public string? Salary { get; set; }
            public string? JobTime { get; set; }
            public string? Experience { get; set; }
            public string? ContactEmail { get; set; }
            public string? Website { get; set; }
            public DateTime? Deadline { get; set; }
            public List<int> SkillIds { get; set; } = new();
        }

        // Create a new job posting.
        [HttpPost("jobs")]
        public async Task<IActionResult> CreateJob(JobPostRequest req)
        {
            if (string.IsNullOrWhiteSpace(req?.Title))
                return BadRequest(ApiResponse<object>.Fail("Job title is required."));

            var company = await GetMyCompanyAsync();
            if (company == null) return NotFound(ApiResponse<object>.Fail("Company profile not found"));

            var job = new JobPosting { company_id = company.company_id, posted_date = DateTime.Now, status = "Active" };
            _db.JobPostings.Add(job);
            ApplyJobFields(job, req);

            await _db.SaveChangesAsync();

            foreach (var skillId in req.SkillIds.Distinct())
                _db.JobSkills.Add(new JobSkill { job_id = job.job_id, skill_id = skillId });
            await _db.SaveChangesAsync();

            return Ok(ApiResponse<int>.Ok(job.job_id, "Job posted successfully"));
        }

        // Update an existing job posting.
        [HttpPut("jobs/{jobId:int}")]
        public async Task<IActionResult> UpdateJob(int jobId, JobPostRequest req)
        {
            if (string.IsNullOrWhiteSpace(req?.Title))
                return BadRequest(ApiResponse<object>.Fail("Job title is required."));

            var company = await GetMyCompanyAsync();
            if (company == null) return NotFound(ApiResponse<object>.Fail("Company profile not found"));

            var job = await _db.JobPostings
                .Include(j => j.JobSkills)
                .FirstOrDefaultAsync(j => j.job_id == jobId && j.company_id == company.company_id);
            if (job == null) return NotFound(ApiResponse<object>.Fail("Job not found"));

            _db.JobSkills.RemoveRange(job.JobSkills);
            ApplyJobFields(job, req);

            await _db.SaveChangesAsync();

            foreach (var skillId in req.SkillIds.Distinct())
                _db.JobSkills.Add(new JobSkill { job_id = job.job_id, skill_id = skillId });
            await _db.SaveChangesAsync();

            return Ok(ApiResponse<string>.Ok("", "Job updated successfully"));
        }

        private static void ApplyJobFields(JobPosting job, JobPostRequest req)
        {
            job.title = req.Title.Trim();
            job.description = req.Description;
            job.location = req.Location;
            job.industry_type = req.IndustryType;
            job.job_type = req.JobType;
            job.salary = req.Salary;
            job.job_time = req.JobTime;
            job.experience = req.Experience;
            job.contact_email = req.ContactEmail;
            job.website = req.Website;
            job.deadline = req.Deadline;
        }

        [HttpPost("jobs/{jobId:int}/toggle-status")]
        public async Task<IActionResult> ToggleJobStatus(int jobId)
        {
            var company = await GetMyCompanyAsync();
            if (company == null) return NotFound(ApiResponse<object>.Fail("Company profile not found"));

            var job = await _db.JobPostings.FirstOrDefaultAsync(j => j.job_id == jobId && j.company_id == company.company_id);
            if (job == null) return NotFound(ApiResponse<object>.Fail("Job not found"));

            job.status = job.status == "Active" ? "Closed" : "Active";
            await _db.SaveChangesAsync();

            return Ok(ApiResponse<string>.Ok(job.status, "Status updated"));
        }

        [HttpDelete("jobs/{jobId:int}")]
        public async Task<IActionResult> DeleteJob(int jobId)
        {
            var company = await GetMyCompanyAsync();
            if (company == null) return NotFound(ApiResponse<object>.Fail("Company profile not found"));

            var job = await _db.JobPostings.FirstOrDefaultAsync(j => j.job_id == jobId && j.company_id == company.company_id);
            if (job == null) return NotFound(ApiResponse<object>.Fail("Job not found"));

            try
            {
                _db.JobApplications.RemoveRange(_db.JobApplications.Where(a => a.job_id == jobId));
                _db.JobSkills.RemoveRange(_db.JobSkills.Where(js => js.job_id == jobId));
                _db.JobMatchings.RemoveRange(_db.JobMatchings.Where(m => m.job_id == jobId));
                _db.JobPostings.Remove(job);
                await _db.SaveChangesAsync();
                return Ok(ApiResponse<string>.Ok("", "Deleted"));
            }
            catch (Exception)
            {
                return BadRequest(ApiResponse<string>.Fail("Could not delete this job posting."));
            }
        }

        // ══════════════════════════════════════════════
        // APPLICATIONS
        // ══════════════════════════════════════════════
        [HttpGet("applications")]
        public async Task<IActionResult> Applications([FromQuery] int? jobId, [FromQuery] string? status, [FromQuery] int page = 1)
        {
            var company = await GetMyCompanyAsync();
            if (company == null) return NotFound(ApiResponse<object>.Fail("Company profile not found"));

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
                .Select(a => new
                {
                    applicationId = a.application_id,
                    jobId = a.job_id,
                    jobTitle = a.JobPosting.title,
                    studentName = a.Student.User.name,
                    studentEmail = a.Student.User.email,
                    status = a.status,
                    resumeUrl = a.resume_url,
                    coverLetter = a.cover_letter,
                    appliedAt = a.applied_at
                })
                .ToListAsync();

            var jobOptions = await _db.JobPostings
                .Where(j => j.company_id == company.company_id)
                .OrderByDescending(j => j.posted_date)
                .Select(j => new { jobId = j.job_id, title = j.title })
                .ToListAsync();

            return Ok(ApiResponse<object>.Ok(new
            {
                applications,
                jobOptions,
                jobIdFilter = jobId,
                statusFilter = status ?? "All",
                page,
                pageSize,
                totalCount
            }));
        }

        // Read-only, scoped to the company's own job postings.
        [HttpGet("applications/{applicationId:int}")]
        public async Task<IActionResult> ApplicantProfile(int applicationId)
        {
            var company = await GetMyCompanyAsync();
            if (company == null) return NotFound(ApiResponse<object>.Fail("Company profile not found"));

            var myJobIds = _db.JobPostings.Where(j => j.company_id == company.company_id).Select(j => j.job_id);

            var app = await _db.JobApplications
                .Include(a => a.JobPosting)
                .Include(a => a.Student).ThenInclude(s => s.User)
                .FirstOrDefaultAsync(a => a.application_id == applicationId && myJobIds.Contains(a.job_id));

            if (app == null) return NotFound(ApiResponse<object>.Fail("Not found"));

            var skills = await (
                from ss in _db.StudentSkills
                join sk in _db.Skills on ss.skill_id equals sk.skill_id
                where ss.student_id == app.student_id
                select new { skillId = sk.skill_id, skillName = sk.skill_name, proficiencyLevel = ss.proficiency_level }
            ).ToListAsync();

            var p = app.Student;
            return Ok(ApiResponse<object>.Ok(new
            {
                applicationId = app.application_id,
                jobTitle = app.JobPosting.title,
                status = app.status,
                coverLetter = app.cover_letter,
                resumeUrl = !string.IsNullOrWhiteSpace(app.resume_url) ? app.resume_url : p.resume_url,
                appliedAt = app.applied_at,
                name = p.User.name,
                email = p.User.email,
                phone = p.phone,
                city = p.city,
                bio = p.bio,
                educationLevel = p.education_level,
                program = p.program,
                universityName = p.university_name,
                fieldOfStudy = p.field_of_study,
                interests = p.interests,
                goal = p.goal,
                linkedinUrl = p.linkedin_url,
                skills
            }));
        }

        public class UpdateAppStatusRequest { public string Status { get; set; } = ""; }

        [HttpPut("applications/{applicationId:int}/status")]
        public async Task<IActionResult> UpdateApplicationStatus(int applicationId, UpdateAppStatusRequest req)
        {
            if (req == null || req.Status is not ("Pending" or "Reviewed" or "Rejected"))
                return BadRequest(ApiResponse<object>.Fail("Invalid status"));

            var company = await GetMyCompanyAsync();
            if (company == null) return NotFound(ApiResponse<object>.Fail("Company profile not found"));

            var myJobIds = _db.JobPostings.Where(j => j.company_id == company.company_id).Select(j => j.job_id);
            var app = await _db.JobApplications.FirstOrDefaultAsync(a => a.application_id == applicationId && myJobIds.Contains(a.job_id));
            if (app == null) return NotFound(ApiResponse<object>.Fail("Application not found"));

            app.status = req.Status;
            app.reviewed_at = DateTime.Now;
            await _db.SaveChangesAsync();

            // Notify the student. Best-effort: the status update above has already
            // been saved, so a problem creating the notification (e.g. a database
            // CHECK constraint on Notifications.type) must NOT fail this action.
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
                // Swallow — see note above.
            }

            return Ok(ApiResponse<string>.Ok(app.status, "Updated"));
        }

        // ══════════════════════════════════════════════
        // COMPANY PROFILE
        // ══════════════════════════════════════════════
        [HttpGet("profile")]
        public async Task<IActionResult> Profile()
        {
            var user = await _db.Users.Include(u => u.CompanyProfile).FirstOrDefaultAsync(u => u.user_id == UserId);
            if (user?.CompanyProfile == null) return NotFound(ApiResponse<object>.Fail("Company profile not found"));

            var c = user.CompanyProfile;
            return Ok(ApiResponse<object>.Ok(new
            {
                companyName = c.company_name,
                email = user.email,
                industry = c.industry,
                location = c.location,
                description = c.description,
                website = c.website,
                logoUrl = c.logo_url,
                employeeCount = c.employee_count,
                foundedYear = c.founded_year,
                isVerified = c.is_verified,
                createdAt = user.created_at,
                uniqueKey = user.unique_key
            }));
        }

        public class CompanyProfileUpdateRequest
        {
            public string CompanyName { get; set; } = "";
            public string? Industry { get; set; }
            public string? Location { get; set; }
            public string? Description { get; set; }
            public string? Website { get; set; }
            public string? LogoUrl { get; set; }
            public int? EmployeeCount { get; set; }
            public int? FoundedYear { get; set; }
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile(CompanyProfileUpdateRequest req)
        {
            if (string.IsNullOrWhiteSpace(req?.CompanyName))
                return BadRequest(ApiResponse<object>.Fail("Company name is required."));

            var user = await _db.Users.Include(u => u.CompanyProfile).FirstOrDefaultAsync(u => u.user_id == UserId);
            if (user?.CompanyProfile == null) return NotFound(ApiResponse<object>.Fail("Company profile not found"));

            var c = user.CompanyProfile;
            c.company_name = req.CompanyName.Trim();
            c.industry = req.Industry;
            c.location = req.Location;
            c.description = req.Description;
            c.website = req.Website;
            c.logo_url = req.LogoUrl;
            c.employee_count = req.EmployeeCount?.ToString();
            c.founded_year = req.FoundedYear;
            // is_verified is intentionally NOT editable here — only Admin can verify a company

            await _db.SaveChangesAsync();

            return Ok(ApiResponse<string>.Ok("", "Company profile updated successfully"));
        }
    }
}

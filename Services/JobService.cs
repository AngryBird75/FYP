using AspiraHub.Data;
using AspiraHub.Models;
using AspiraHub.ViewModels.Job;
using Microsoft.EntityFrameworkCore;

namespace AspiraHub.Services
{
    public class JobService : IJobService
    {
        private readonly AppDbContext _db;

        public JobService(AppDbContext db)
        {
            _db = db;
        }

        // ══════════════════════════════════════════════════════════
        // BROWSE JOBS — list active jobs with match score
        // ══════════════════════════════════════════════════════════
        public async Task<JobBrowseVM> BrowseJobsAsync(int studentId, string? search, string? jobType)
        {
            var query = _db.JobPostings.Where(j => j.status == "Active").AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(j => j.title.Contains(search));

            if (!string.IsNullOrWhiteSpace(jobType))
                query = query.Where(j => j.job_type == jobType);

            var jobs = await query
                .Include(j => j.Company)
                .OrderByDescending(j => j.posted_date)
                .ToListAsync();

            var studentProfile = await _db.StudentProfiles.FindAsync(studentId);
            int userId = studentProfile?.user_id ?? 0;

            var appliedJobIds = await _db.JobApplications
                .Where(a => a.student_id == studentId)
                .Select(a => a.job_id)
                .ToListAsync();

            var savedJobIds = await _db.SavedItems
                .Where(s => s.user_id == userId && s.item_type == "Job")
                .Select(s => s.item_id)
                .ToListAsync();

            var matchScores = await _db.JobMatchings
                .Where(m => m.student_id == studentId)
                .ToDictionaryAsync(m => m.job_id, m => m.match_score ?? 0);

            var cards = jobs.Select(j => new JobCardVM
            {
                JobId = j.job_id,
                Title = j.title,
                CompanyName = j.Company?.company_name ?? "",
                Location = j.location ?? "",
                JobType = j.job_type ?? "",
                Salary = j.salary,
                Experience = j.experience,
                Deadline = j.deadline,
                MatchScore = matchScores.GetValueOrDefault(j.job_id, 0),
                IsApplied = appliedJobIds.Contains(j.job_id),
                IsSaved = savedJobIds.Contains(j.job_id)
            })
            .OrderByDescending(c => c.MatchScore)
            .ToList();

            return new JobBrowseVM
            {
                Jobs = cards,
                SearchTerm = search,
                FilterJobType = jobType
            };
        }

        // ══════════════════════════════════════════════════════════
        // SAVED JOBS
        // ══════════════════════════════════════════════════════════
        public async Task<List<JobCardVM>> GetSavedJobsAsync(int userId, int studentId)
        {
            var savedJobIds = await _db.SavedItems
                .Where(s => s.user_id == userId && s.item_type == "Job")
                .Select(s => s.item_id)
                .ToListAsync();

            var jobs = await _db.JobPostings
                .Where(j => savedJobIds.Contains(j.job_id))
                .Include(j => j.Company)
                .ToListAsync();

            var appliedJobIds = await _db.JobApplications
                .Where(a => a.student_id == studentId)
                .Select(a => a.job_id)
                .ToListAsync();

            var matchScores = await _db.JobMatchings
                .Where(m => m.student_id == studentId)
                .ToDictionaryAsync(m => m.job_id, m => m.match_score ?? 0);

            return jobs.Select(j => new JobCardVM
            {
                JobId = j.job_id,
                Title = j.title,
                CompanyName = j.Company?.company_name ?? "",
                Location = j.location ?? "",
                JobType = j.job_type ?? "",
                Salary = j.salary,
                Deadline = j.deadline,
                MatchScore = matchScores.GetValueOrDefault(j.job_id, 0),
                IsApplied = appliedJobIds.Contains(j.job_id),
                IsSaved = true
            }).ToList();
        }

        // ══════════════════════════════════════════════════════════
        // MY APPLICATIONS
        // ══════════════════════════════════════════════════════════
        public async Task<List<MyApplicationVM>> GetMyApplicationsAsync(int studentId)
        {
            return await _db.JobApplications
                .Where(a => a.student_id == studentId)
                .Include(a => a.JobPosting)
                    .ThenInclude(j => j.Company)
                .OrderByDescending(a => a.applied_at)
                .Select(a => new MyApplicationVM
                {
                    ApplicationId = a.application_id,
                    JobTitle = a.JobPosting.title,
                    CompanyName = a.JobPosting.Company.company_name,
                    Status = a.status,
                    AppliedAt = a.applied_at
                })
                .ToListAsync();
        }

        // ══════════════════════════════════════════════════════════
        // APPLY FOR A JOB
        // ══════════════════════════════════════════════════════════
        public async Task<bool> ApplyJobAsync(int studentId, int jobId, string? coverLetter)
        {
            bool alreadyApplied = await _db.JobApplications
                .AnyAsync(a => a.student_id == studentId && a.job_id == jobId);

            if (alreadyApplied) return false;

            _db.JobApplications.Add(new JobApplication
            {
                student_id = studentId,
                job_id = jobId,
                status = "Pending",
                cover_letter = coverLetter,
                applied_at = DateTime.Now
            });

            var job = await _db.JobPostings.FindAsync(jobId);
            if (job != null)
                job.applications_count += 1;

            await _db.SaveChangesAsync();
            return true;
        }

        // ══════════════════════════════════════════════════════════
        // SAVE / UNSAVE JOB
        // ══════════════════════════════════════════════════════════
        public async Task<bool> SaveJobAsync(int userId, int jobId)
        {
            bool exists = await _db.SavedItems.AnyAsync(s =>
                s.user_id == userId && s.item_type == "Job" && s.item_id == jobId);

            if (exists) return true;

            _db.SavedItems.Add(new SavedItem
            {
                user_id = userId,
                item_type = "Job",
                item_id = jobId,
                saved_at = DateTime.Now
            });
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnsaveJobAsync(int userId, int jobId)
        {
            var item = await _db.SavedItems.FirstOrDefaultAsync(s =>
                s.user_id == userId && s.item_type == "Job" && s.item_id == jobId);

            if (item == null) return false;

            _db.SavedItems.Remove(item);
            await _db.SaveChangesAsync();
            return true;
        }

        // ══════════════════════════════════════════════════════════
        // MATCHING ALGORITHM — runs for one student against all jobs
        // match_score = (matched job skills / total job skills) * 100
        // ══════════════════════════════════════════════════════════
        public async Task RunMatchingForStudentAsync(int studentId)
        {
            var studentSkillIds = await _db.StudentSkills
                .Where(ss => ss.student_id == studentId)
                .Select(ss => ss.skill_id)
                .ToHashSetAsync();

            var activeJobs = await _db.JobPostings
                .Where(j => j.status == "Active")
                .ToListAsync();

            foreach (var job in activeJobs)
            {
                var jobSkillIds = await _db.JobSkills
                    .Where(js => js.job_id == job.job_id)
                    .Select(js => js.skill_id)
                    .ToListAsync();

                int score = 0;
                if (jobSkillIds.Any())
                {
                    int matched = jobSkillIds.Count(id => studentSkillIds.Contains(id));
                    score = (int)Math.Round((double)matched / jobSkillIds.Count * 100);
                }

                var existing = await _db.JobMatchings
                    .FirstOrDefaultAsync(m => m.student_id == studentId && m.job_id == job.job_id);

                if (existing != null)
                {
                    existing.match_score = score;
                    existing.matched_at = DateTime.Now;
                }
                else
                {
                    _db.JobMatchings.Add(new JobMatching
                    {
                        student_id = studentId,
                        job_id = job.job_id,
                        match_score = score,
                        matched_at = DateTime.Now
                    });
                }
            }

            await _db.SaveChangesAsync();
        }
    }
}
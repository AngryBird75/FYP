using AspiraHub.Data;
using AspiraHub.Models;
using AspiraHub.ViewModels.Dashboard;
using Microsoft.EntityFrameworkCore;

namespace AspiraHub.Repositories
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly AppDbContext _db;

        public DashboardRepository(AppDbContext db)
        {
            _db = db;
        }

        // ══════════════════════════════════════════════
        // STUDENT DASHBOARD
        // ══════════════════════════════════════════════
        public async Task<StudentDashboardVM> GetStudentDashboardAsync(int userId)
        {
            var user = await _db.Users
                .Include(u => u.StudentProfile)
                .FirstOrDefaultAsync(u => u.user_id == userId);

            if (user == null)
                return new StudentDashboardVM();

            var student = user.StudentProfile;
            if (student == null)
                return new StudentDashboardVM { Name = user.name };

            // Skills
            var skills = await _db.StudentSkills
                .Include(ss => ss.User)
                .Where(ss => ss.student_id == student.student_id)
                .Join(_db.Skills,
                      ss => ss.skill_id,
                      s => s.skill_id,
                      (ss, s) => new StudentSkillVM
                      {
                          SkillName = s.skill_name,
                          ProficiencyLevel = ss.proficiency_level
                      })
                .ToListAsync();

            // Courses
            var coursesInProgress = await _db.StudentCourses
                .CountAsync(sc => sc.student_id == student.student_id
                               && sc.status == "Enrolled");
            var coursesCompleted = await _db.StudentCourses
                .CountAsync(sc => sc.student_id == student.student_id
                               && sc.status == "Completed");

            // Roadmaps
            var roadmaps = await _db.Roadmaps
                .Where(r => r.student_id == student.student_id)
                .ToListAsync();

            int roadmapProgress = 0;
            if (roadmaps.Any())
            {
                var roadmapIds = roadmaps.Select(r => r.roadmap_id).ToList();
                var totalSteps = await _db.RoadmapSteps
                    .CountAsync(rs => roadmapIds.Contains(rs.roadmap_id));
                var doneSteps = await _db.RoadmapProgresses
                    .CountAsync(rp => rp.student_id == student.student_id
                                   && rp.status == "Completed");
                roadmapProgress = totalSteps > 0
                    ? (int)Math.Round((double)doneSteps / totalSteps * 100)
                    : 0;
            }

            // Jobs
            var matchedJobs = await _db.JobMatchings
                .CountAsync(jm => jm.student_id == student.student_id);
            var appliedJobs = await _db.JobApplications
                .CountAsync(ja => ja.student_id == student.student_id);

            // Top matched jobs
            var topJobs = await _db.JobMatchings
                .Where(jm => jm.student_id == student.student_id)
                .OrderByDescending(jm => jm.match_score)
                .Take(3)
                .Include(jm => jm.JobPosting)
                    .ThenInclude(jp => jp.Company)
                .Select(jm => new MatchedJobVM
                {
                    JobId = jm.job_id,
                    Title = jm.JobPosting.title,
                    Company = jm.JobPosting.Company.company_name,
                    Location = jm.JobPosting.location ?? "",
                    MatchScore = jm.match_score ?? 0,
                    JobType = jm.JobPosting.job_type ?? "",
                    Deadline = jm.JobPosting.deadline
                })
                .ToListAsync();

            // Notifications
            var notifs = await _db.Notifications
                .Where(n => n.user_id == userId)
                .OrderByDescending(n => n.created_at)
                .Take(5)
                .ToListAsync();
            var unread = notifs.Count(n => !n.is_read);

            // Announcements
            var announcements = await _db.Announcements
                .Where(a => a.is_active
                         && (a.target_role == null
                          || a.target_role == "Student"
                          || a.target_role == "All")
                         && (a.expires_at == null || a.expires_at > DateTime.Now))
                .OrderByDescending(a => a.published_at)
                .Take(3)
                .ToListAsync();

            return new StudentDashboardVM
            {
                Name = user.name,
                UniqueKey = user.unique_key ?? "",
                EducationLevel = student.education_level ?? "",
                UniversityName = student.university_name ?? "",
                Program = student.program ?? "",
                Interests = student.interests ?? "",
                Goal = student.goal ?? "",
                ProfileCompletion = student.profile_completion,
                ProfilePicture = user.profile_picture,
                Skills = skills,
                CoursesInProgress = coursesInProgress,
                CoursesCompleted = coursesCompleted,
                TotalRoadmaps = roadmaps.Count,
                RoadmapProgress = roadmapProgress,
                MatchedJobs = matchedJobs,
                AppliedJobs = appliedJobs,
                TopMatchedJobs = topJobs,
                UnreadNotifications = unread,
                RecentNotifications = notifs,
                Announcements = announcements
            };
        }

        // ══════════════════════════════════════════════
        // COMPANY DASHBOARD
        // ══════════════════════════════════════════════
        public async Task<CompanyDashboardVM> GetCompanyDashboardAsync(int userId)
        {
            var user = await _db.Users
                .Include(u => u.CompanyProfile)
                .FirstOrDefaultAsync(u => u.user_id == userId);

            if (user?.CompanyProfile == null)
                return new CompanyDashboardVM();

            var company = user.CompanyProfile;

            // Jobs
            var jobs = await _db.JobPostings
                .Where(j => j.company_id == company.company_id)
                .OrderByDescending(j => j.posted_date)
                .ToListAsync();

            var activeJobs = jobs.Count(j => j.status == "Active");
            var totalViews = jobs.Sum(j => j.views_count);
            var totalApplications = jobs.Sum(j => j.applications_count);
            var pendingApplications = await _db.JobApplications
                .Where(ja => jobs.Select(j => j.job_id).Contains(ja.job_id)
                          && ja.status == "Pending")
                .CountAsync();

            // Jobs list VM
            var jobsVM = jobs.Take(10).Select(j => new CompanyJobVM
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
            }).ToList();

            // Recent Applications
            var recentApps = await _db.JobApplications
                .Where(ja => jobs.Select(j => j.job_id).Contains(ja.job_id))
                .OrderByDescending(ja => ja.applied_at)
                .Take(5)
                .Include(ja => ja.Student)
                    .ThenInclude(s => s.User)
                .Include(ja => ja.JobPosting)
                .Select(ja => new RecentApplicationVM
                {
                    ApplicationId = ja.application_id,
                    StudentName = ja.Student.User.name,
                    JobTitle = ja.JobPosting.title,
                    Status = ja.status,
                    AppliedAt = ja.applied_at,
                    ResumeUrl = ja.resume_url
                })
                .ToListAsync();

            // Notifications
            var notifs = await _db.Notifications
                .Where(n => n.user_id == userId)
                .OrderByDescending(n => n.created_at)
                .Take(5)
                .ToListAsync();

            return new CompanyDashboardVM
            {
                CompanyName = company.company_name,
                Industry = company.industry ?? "",
                Location = company.location ?? "",
                LogoUrl = company.logo_url,
                IsVerified = company.is_verified,
                TotalJobs = jobs.Count,
                ActiveJobs = activeJobs,
                TotalApplications = totalApplications,
                PendingApplications = pendingApplications,
                TotalViews = totalViews,
                Jobs = jobsVM,
                RecentApplications = recentApps,
                UnreadNotifications = notifs.Count(n => !n.is_read),
                RecentNotifications = notifs
            };
        }

        // ══════════════════════════════════════════════
        // ADMIN DASHBOARD
        // ══════════════════════════════════════════════
        public async Task<AdminDashboardVM> GetAdminDashboardAsync()
        {
            var totalUsers = await _db.Users.CountAsync();
            var students = await _db.Users.CountAsync(u => u.role == "Student");
            var companies = await _db.Users.CountAsync(u => u.role == "Company");
            var admins = await _db.Users.CountAsync(u => u.role == "Admin");
            var totalJobs = await _db.JobPostings.CountAsync();
            var totalApps = await _db.JobApplications.CountAsync();
            var totalCareers = await _db.Careers.CountAsync();
            var totalSkills = await _db.Skills.CountAsync();
            var activeAnnouncements = await _db.Announcements
                .CountAsync(a => a.is_active);

            // Recent Users
            var recentUsers = await _db.Users
                .OrderByDescending(u => u.created_at)
                .Take(8)
                .Select(u => new RecentUserVM
                {
                    UserId = u.user_id,
                    Name = u.name,
                    Email = u.email,
                    Role = u.role,
                    IsActive = u.is_active,
                    CreatedAt = u.created_at
                })
                .ToListAsync();

            // Recent Jobs
            var recentJobs = await _db.JobPostings
                .OrderByDescending(j => j.posted_date)
                .Take(5)
                .Include(j => j.Company)
                .Select(j => new AdminJobVM
                {
                    JobId = j.job_id,
                    Title = j.title,
                    Company = j.Company.company_name,
                    Status = j.status,
                    Applications = j.applications_count,
                    PostedDate = j.posted_date
                })
                .ToListAsync();

            // Announcements
            var announcements = await _db.Announcements
                .OrderByDescending(a => a.published_at)
                .Take(5)
                .ToListAsync();

            // Admin Logs
            var logs = await _db.AdminLogs
                .OrderByDescending(l => l.created_at)
                .Take(8)
                .ToListAsync();

            return new AdminDashboardVM
            {
                TotalUsers = totalUsers,
                TotalStudents = students,
                TotalCompanies = companies,
                TotalJobs = totalJobs,
                TotalApplications = totalApps,
                TotalCareers = totalCareers,
                TotalSkills = totalSkills,
                ActiveAnnouncements = activeAnnouncements,
                RecentUsers = recentUsers,
                RecentJobs = recentJobs,
                Announcements = announcements,
                RecentLogs = logs,
                StudentCount = students,
                CompanyCount = companies,
                AdminCount = admins
            };
        }

        // ══════════════════════════════════════════════
        // SHARED
        // ══════════════════════════════════════════════
        public async Task<List<Notification>> GetNotificationsAsync(int userId, int take = 5)
            => await _db.Notifications
                .Where(n => n.user_id == userId)
                .OrderByDescending(n => n.created_at)
                .Take(take)
                .ToListAsync();

        public async Task MarkNotificationReadAsync(int notifId, int userId)
        {
            var notif = await _db.Notifications.FindAsync(notifId);
            // Only the owning user may mark their own notification as read.
            if (notif != null && notif.user_id == userId)
            {
                notif.is_read = true;
                await _db.SaveChangesAsync();
            }
        }

        public async Task<int> GetUnreadCountAsync(int userId)
            => await _db.Notifications
                .CountAsync(n => n.user_id == userId && !n.is_read);
    }
}
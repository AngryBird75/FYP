using AspiraHub.Models;
using Microsoft.EntityFrameworkCore;

namespace AspiraHub.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<StudentProfile> StudentProfiles { get; set; }
        public DbSet<CompanyProfile> CompanyProfiles { get; set; }
        public DbSet<Career> Careers { get; set; }
        public DbSet<Skill> Skills { get; set; }
        public DbSet<StudentSkill> StudentSkills { get; set; }
        public DbSet<JobPosting> JobPostings { get; set; }
        public DbSet<JobSkill> JobSkills { get; set; }
        public DbSet<JobApplication> JobApplications { get; set; }
        public DbSet<JobMatching> JobMatchings { get; set; }
        public DbSet<Roadmap> Roadmaps { get; set; }
        public DbSet<RoadmapStep> RoadmapSteps { get; set; }
        public DbSet<RoadmapProgress> RoadmapProgresses { get; set; }
        public DbSet<StudentCourse> StudentCourses { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Announcement> Announcements { get; set; }
        public DbSet<AdminLog> AdminLogs { get; set; }
        // Add these DbSets to existing AppDbContext class
        public DbSet<Institute> Institutes { get; set; }
public DbSet<InstituteCourse> InstituteCourses { get; set; }
        public DbSet<DegreeProgram> DegreePrograms { get; set; }
        public DbSet<SemesterMilestone> SemesterMilestones { get; set; }
        // Add to existing AppDbContext class

        public DbSet<CareerSkill> CareerSkills { get; set; }
        public DbSet<SkillGapAnalysis> SkillGapAnalyses { get; set; }
        public DbSet<CareerComparison> CareerComparisons { get; set; }
        public DbSet<SavedItem> SavedItems { get; set; }
        public DbSet<RoadmapTemplate> RoadmapTemplates { get; set; }
        public DbSet<RoadmapTemplateStep> RoadmapTemplateSteps { get; set; }
        public DbSet<University> Universities { get; set; }
        public DbSet<PasswordReset> PasswordResets { get; set; }
        public DbSet<CourseSkill> CourseSkills { get; set; }
        public DbSet<ProgramSkill> ProgramSkills { get; set; }
        public DbSet<InstituteLocation> InstituteLocations { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Users — unique email
            modelBuilder.Entity<User>()
                .HasIndex(u => u.email)
                .IsUnique();

            // Users — unique key
            modelBuilder.Entity<User>()
                .HasIndex(u => u.unique_key)
                .IsUnique();

            // User → StudentProfile (1-to-1)
            modelBuilder.Entity<User>()
                .HasOne(u => u.StudentProfile)
                .WithOne(s => s.User)
                .HasForeignKey<StudentProfile>(s => s.user_id)
                .OnDelete(DeleteBehavior.Cascade);

            // User → CompanyProfile (1-to-1)
            modelBuilder.Entity<User>()
                .HasOne(u => u.CompanyProfile)
                .WithOne(c => c.User)
                .HasForeignKey<CompanyProfile>(c => c.user_id)
                .OnDelete(DeleteBehavior.Cascade);

            // StudentProfile → Roadmaps
            modelBuilder.Entity<Roadmap>()
                .HasOne(r => r.Student)
                .WithMany(s => s.Roadmaps)
                .HasForeignKey(r => r.student_id)
                .OnDelete(DeleteBehavior.Cascade);

            // StudentProfile → JobApplications
            modelBuilder.Entity<JobApplication>()
                .HasOne(a => a.Student)
                .WithMany(s => s.JobApplications)
                .HasForeignKey(a => a.student_id)
                .OnDelete(DeleteBehavior.NoAction);

            // JobPosting → JobApplications
            modelBuilder.Entity<JobApplication>()
                .HasOne(a => a.JobPosting)
                .WithMany(j => j.JobApplications)
                .HasForeignKey(a => a.job_id)
                .OnDelete(DeleteBehavior.Cascade);

            // CompanyProfile → JobPostings
            modelBuilder.Entity<JobPosting>()
                .HasOne(j => j.Company)
                .WithMany()
                .HasForeignKey(j => j.company_id)
                .OnDelete(DeleteBehavior.Cascade);

            // JobPosting → JobSkills
            modelBuilder.Entity<JobSkill>()
                .HasOne(js => js.JobPosting)
                .WithMany(j => j.JobSkills)
                .HasForeignKey(js => js.job_id)
                .OnDelete(DeleteBehavior.Cascade);

            // JobPosting → JobMatching
            modelBuilder.Entity<JobMatching>()
                .HasOne(m => m.JobPosting)
                .WithMany(j => j.JobMatchings)
                .HasForeignKey(m => m.job_id)
                .OnDelete(DeleteBehavior.Cascade);

            // StudentSkill — no cascade conflict
            modelBuilder.Entity<StudentSkill>()
                .HasOne(ss => ss.User)
                .WithMany()
                .HasForeignKey(ss => ss.student_id)
                .OnDelete(DeleteBehavior.NoAction);

            // CourseSkill — accurate Course <-> Skill link (replaces text-search matching)
            modelBuilder.Entity<CourseSkill>()
                .HasOne(cs => cs.Course)
                .WithMany(c => c.CourseSkills)
                .HasForeignKey(cs => cs.course_id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CourseSkill>()
                .HasOne(cs => cs.Skill)
                .WithMany()
                .HasForeignKey(cs => cs.skill_id)
                .OnDelete(DeleteBehavior.NoAction);

            // ProgramSkill — which skills belong to which degree program/department
            modelBuilder.Entity<ProgramSkill>()
                .HasOne(ps => ps.Program)
                .WithMany()
                .HasForeignKey(ps => ps.program_id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProgramSkill>()
                .HasOne(ps => ps.Skill)
                .WithMany()
                .HasForeignKey(ps => ps.skill_id)
                .OnDelete(DeleteBehavior.NoAction);

            // StudentProfile -> University (validated dropdown selection, Step2)
            modelBuilder.Entity<StudentProfile>()
                .HasOne(s => s.University)
                .WithMany()
                .HasForeignKey(s => s.university_id)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspiraHub.Models
{
    [Table("JobPostings")]
    public class JobPosting
    {
        [Key]
        [Column("job_id")]
        public int job_id { get; set; }

        [Column("company_id")]
        public int company_id { get; set; }

        [Required, MaxLength(200)]
        [Column("title")]
        public string title { get; set; } = "";

        [Column("description")]
        public string? description { get; set; }

        [MaxLength(100)]
        [Column("location")]
        public string? location { get; set; }

        [MaxLength(100)]
        [Column("industry_type")]
        public string? industry_type { get; set; }

        [MaxLength(50)]
        [Column("job_type")]
        public string? job_type { get; set; }     // Internship / Full-time / Part-time

        [MaxLength(100)]
        [Column("salary")]
        public string? salary { get; set; }

        [MaxLength(50)]
        [Column("job_time")]
        public string? job_time { get; set; }

        [MaxLength(100)]
        [Column("experience")]
        public string? experience { get; set; }

        [MaxLength(100)]
        [Column("contact_email")]
        public string? contact_email { get; set; }

        [MaxLength(200)]
        [Column("website")]
        public string? website { get; set; }

        [Required, MaxLength(20)]
        [Column("status")]
        public string status { get; set; } = "Active";

        [Column("deadline")]
        public DateTime? deadline { get; set; }

        [Column("posted_date")]
        public DateTime posted_date { get; set; } = DateTime.Now;

        [Column("views_count")]
        public int views_count { get; set; } = 0;

        [Column("applications_count")]
        public int applications_count { get; set; } = 0;

        [ForeignKey("company_id")]
        public CompanyProfile Company { get; set; }

        public ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();
        public ICollection<JobSkill> JobSkills { get; set; } = new List<JobSkill>();
        public ICollection<JobMatching> JobMatchings { get; set; } = new List<JobMatching>();
    }
}
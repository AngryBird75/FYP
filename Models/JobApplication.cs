using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspiraHub.Models
{
    [Table("JobApplications")]
    public class JobApplication
    {
        [Key]
        [Column("application_id")]
        public int application_id { get; set; }

        [Column("job_id")]
        public int job_id { get; set; }

        [Column("student_id")]
        public int student_id { get; set; }

        [Required, MaxLength(20)]
        [Column("status")]
        public string status { get; set; } = "Pending";

        [Column("cover_letter")]
        public string? cover_letter { get; set; }

        [MaxLength(300)]
        [Column("resume_url")]
        public string? resume_url { get; set; }

        [Column("applied_at")]
        public DateTime applied_at { get; set; } = DateTime.Now;

        [Column("reviewed_at")]
        public DateTime? reviewed_at { get; set; }

        [ForeignKey("job_id")]
        public JobPosting JobPosting { get; set; }

        [ForeignKey("student_id")]
        public StudentProfile Student { get; set; }
    }
}
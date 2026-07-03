using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspiraHub.Models
{
    [Table("JobMatching")]
    public class JobMatching
    {
        [Key]
        [Column("id")]
        public int id { get; set; }

        [Column("job_id")]
        public int job_id { get; set; }

        [Column("student_id")]
        public int student_id { get; set; }

        [Column("match_score")]
        public int? match_score { get; set; }

        [Column("matched_at")]
        public DateTime matched_at { get; set; } = DateTime.Now;

        [ForeignKey("job_id")]
        public JobPosting JobPosting { get; set; }
    }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspiraHub.Models
{
    [Table("Roadmaps")]
    public class Roadmap
    {
        [Key]
        [Column("roadmap_id")]
        public int roadmap_id { get; set; }

        [Column("student_id")]
        public int student_id { get; set; }

        [Column("career_id")]
        public int? career_id { get; set; }

        [Column("template_id")]
        public int? template_id { get; set; }

        [Required, MaxLength(200)]
        [Column("title")]
        public string title { get; set; } = "";

        [Required, MaxLength(20)]
        [Column("type")]
        public string type { get; set; } = "Career";   // Career / Education / Mixed

        [MaxLength(20)]
        [Column("status")]
        public string status { get; set; } = "Active";  // Active / Paused / Completed

        [Column("created_at")]
        public DateTime created_at { get; set; } = DateTime.Now;

        [Column("updated_at")]
        public DateTime? updated_at { get; set; }

        [ForeignKey("student_id")]
        public StudentProfile Student { get; set; }

        [ForeignKey("career_id")]
        public Career? Career { get; set; }
        [Column("duration_months")]
        public int duration_months { get; set; } = 6;

        [Column("target_end_date")]
        public DateTime? target_end_date { get; set; }
    }
}
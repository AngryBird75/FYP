using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspiraHub.Models
{
    [Table("SemesterMilestones")]
    public class SemesterMilestone
    {
        [Key]
        [Column("milestone_id")]
        public int milestone_id { get; set; }

        [Column("program_id")]
        public int program_id { get; set; }

        [Column("semester_number")]
        public int semester_number { get; set; }

        [Required, MaxLength(200)]
        [Column("title")]
        public string title { get; set; } = "";

        [Column("description")]
        public string? description { get; set; }

        [Required, MaxLength(50)]
        [Column("milestone_type")]
        public string milestone_type { get; set; } = "Academic";

        [Column("is_active")]
        public bool is_active { get; set; } = true;
    }
}
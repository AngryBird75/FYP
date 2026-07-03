using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspiraHub.Models
{
    [Table("RoadmapProgress")]
    public class RoadmapProgress
    {
        [Key]
        [Column("id")]
        public int id { get; set; }

        [Column("step_id")]
        public int step_id { get; set; }

        [Column("student_id")]
        public int student_id { get; set; }

        [Required, MaxLength(20)]
        [Column("status")]
        public string status { get; set; } = "Pending";  // Pending / InProgress / Completed

        [Column("completed_at")]
        public DateTime? completed_at { get; set; }
    }
}
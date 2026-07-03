using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspiraHub.Models
{
    [Table("SkillGapAnalysis")]
    public class SkillGapAnalysis
    {
        [Key]
        [Column("id")]
        public int id { get; set; }

        [Column("student_id")]
        public int student_id { get; set; }

        [Column("career_id")]
        public int career_id { get; set; }

        [Column("skill_id")]
        public int skill_id { get; set; }

        [MaxLength(20)]
        [Column("required_level")]
        public string? required_level { get; set; }

        [MaxLength(20)]
        [Column("current_level")]
        public string? current_level { get; set; }

        [MaxLength(20)]
        [Column("gap_level")]
        public string? gap_level { get; set; }

        [Column("analyzed_at")]
        public DateTime analyzed_at { get; set; } = DateTime.Now;

        [ForeignKey("career_id")]
        public Career Career { get; set; }

        [ForeignKey("skill_id")]
        public Skill Skill { get; set; }
    }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspiraHub.Models
{
    [Table("CareerSkills")]
    public class CareerSkill
    {
        [Key]
        [Column("id")]
        public int id { get; set; }

        [Column("career_id")]
        public int career_id { get; set; }

        [Column("skill_id")]
        public int skill_id { get; set; }

        // Critical / High / Medium / Low
        [MaxLength(20)]
        [Column("importance_level")]
        public string? importance_level { get; set; }

        [ForeignKey("career_id")]
        public Career Career { get; set; }

        [ForeignKey("skill_id")]
        public Skill Skill { get; set; }
    }
}
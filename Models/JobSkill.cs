using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspiraHub.Models
{
    [Table("JobSkills")]
    public class JobSkill
    {
        [Key]
        [Column("id")]
        public int id { get; set; }

        [Column("job_id")]
        public int job_id { get; set; }

        [Column("skill_id")]
        public int skill_id { get; set; }

        [ForeignKey("job_id")]
        public JobPosting JobPosting { get; set; }

        [ForeignKey("skill_id")]
        public Skill Skill { get; set; }
    }
}
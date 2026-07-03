using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspiraHub.Models
{
    [Table("StudentSkills")]
    public class StudentSkill
    {
        [Key]
        [Column("id")]
        public int id { get; set; }

        [Column("student_id")]
        public int student_id { get; set; }

        [Column("skill_id")]
        public int skill_id { get; set; }

        [Required, MaxLength(50)]
        [Column("proficiency_level")]
        public string proficiency_level { get; set; } = "";

        // Navigation
        public User? User { get; set; }
    }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspiraHub.Models
{
    // NEW TABLE: defines which skills belong to which degree program/department.
    // Onboarding Step 3 ab is table ke zariye sirf student ke program se
    // relevant skills dikhata hai — poori Skills table nahi.
    [Table("ProgramSkills")]
    public class ProgramSkill
    {
        [Key]
        [Column("id")]
        public int id { get; set; }

        [Column("program_id")]
        public int program_id { get; set; }

        [Column("skill_id")]
        public int skill_id { get; set; }

        [ForeignKey("program_id")]
        public DegreeProgram? Program { get; set; }

        [ForeignKey("skill_id")]
        public Skill? Skill { get; set; }
    }
}

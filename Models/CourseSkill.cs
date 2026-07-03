using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspiraHub.Models
{
    // Links InstituteCourses <-> Skills accurately (DB-driven).
    // Roadmap "Details" aur Recommended Courses ab is table se course
    // dhoondte hain — text-name-match (Contains) ke bajaye.
    [Table("CourseSkills")]
    public class CourseSkill
    {
        [Key]
        [Column("id")]
        public int id { get; set; }

        [Column("course_id")]
        public int course_id { get; set; }

        [Column("skill_id")]
        public int skill_id { get; set; }

        [ForeignKey("course_id")]
        public InstituteCourse? Course { get; set; }

        [ForeignKey("skill_id")]
        public Skill? Skill { get; set; }
    }
}

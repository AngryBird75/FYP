using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspiraHub.Models
{
    [Table("InstituteCourses")]
    public class InstituteCourse
    {
        [Key]
        [Column("course_id")]
        public int course_id { get; set; }

        [Column("institute_id")]
        public int institute_id { get; set; }

        [Required, MaxLength(200)]
        [Column("course_name")]
        public string course_name { get; set; } = "";

        [MaxLength(50)]
        [Column("duration")]
        public string? duration { get; set; }

        [MaxLength(100)]
        [Column("fee")]
        public string? fee { get; set; }

        [MaxLength(200)]
        [Column("certificate_criteria")]
        public string? certificate_criteria { get; set; }

        [ForeignKey("institute_id")]
        public Institute? Institute { get; set; }

        public ICollection<CourseSkill> CourseSkills { get; set; } = new List<CourseSkill>();
    }
}
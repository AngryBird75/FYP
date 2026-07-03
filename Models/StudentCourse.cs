using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspiraHub.Models
{
    [Table("StudentCourses")]
    public class StudentCourse
    {
        [Key]
        [Column("id")]
        public int id { get; set; }

        [Column("student_id")]
        public int student_id { get; set; }

        [Column("course_id")]
        public int course_id { get; set; }

        [Required, MaxLength(20)]
        [Column("status")]
        public string status { get; set; } = "Enrolled";

        [Column("enrolled_at")]
        public DateTime enrolled_at { get; set; } = DateTime.Now;

        [Column("completed_at")]
        public DateTime? completed_at { get; set; }
    }
}
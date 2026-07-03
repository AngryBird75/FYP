using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspiraHub.Models
{
    [Table("StudentProfiles")]
    public class StudentProfile
    {
        [Key]
        [Column("student_id")]
        public int student_id { get; set; }

        [Column("user_id")]
        public int user_id { get; set; }

        [MaxLength(20)]
        [Column("phone")]
        public string? phone { get; set; }

        [MaxLength(100)]
        [Column("city")]
        public string? city { get; set; }

        [MaxLength(500)]
        [Column("bio")]
        public string? bio { get; set; }

        // Step 1
        [MaxLength(50)]
        [Column("education_level")]
        public string? education_level { get; set; }


        // Add these properties to the existing StudentProfile class

        [Column("goal")]
        [MaxLength(200)]
        public string? goal { get; set; }

        [Column("university_name")]
        [MaxLength(200)]
        public string? university_name { get; set; }

        // NEW: validated FK — Step2 ab dropdown se hi university select karwata
        // hai, university_name field wahi se auto-filled hoti hai (display ke liye)
        [Column("university_id")]
        public int? university_id { get; set; }

        [Column("program")]
        [MaxLength(100)]
        public string? program { get; set; }

        [Column("current_semester")]
        public int? current_semester { get; set; }

        [Column("degree_program_id")]
        public int? degree_program_id { get; set; }

        // Step 2
        [MaxLength(100)]
        [Column("field_of_study")]
        public string? field_of_study { get; set; }

        // Step 4
        [MaxLength(255)]
        [Column("interests")]
        public string? interests { get; set; }

        [MaxLength(300)]
        [Column("resume_url")]
        public string? resume_url { get; set; }

        [MaxLength(300)]
        [Column("linkedin_url")]
        public string? linkedin_url { get; set; }

        [Column("profile_completion")]
        public int profile_completion { get; set; } = 0;

       


        // ── Navigation ──
        [ForeignKey("user_id")]
        public User User { get; set; }

        [ForeignKey("university_id")]
        public University? University { get; set; }

       
        public ICollection<Roadmap> Roadmaps { get; set; } = new List<Roadmap>();
        public ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();
    }
}
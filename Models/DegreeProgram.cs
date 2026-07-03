using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspiraHub.Models
{
    [Table("DegreePrograms")]
    public class DegreeProgram
    {
        [Key]
        [Column("program_id")]
        public int program_id { get; set; }

        [Required, MaxLength(100)]
        [Column("program_name")]
        public string program_name { get; set; } = "";

        [MaxLength(200)]
        [Column("full_name")]
        public string? full_name { get; set; }

        [Required, MaxLength(50)]
        [Column("education_level")]
        public string education_level { get; set; } = "";

        [Column("total_semesters")]
        public int total_semesters { get; set; } = 8;

        [Column("is_active")]
        public bool is_active { get; set; } = true;
    }
}
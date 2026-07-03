using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace AspiraHub.Models
{
    [Table("Institutes")]
    public class Institute
    {
        [Key]
        [Column("institute_id")]
        public int institute_id { get; set; }

        [Required, MaxLength(150)]
        [Column("name")]
        public string name { get; set; } = "";

        [MaxLength(50)]
        [Column("type")]
        public string? type { get; set; }

        [MaxLength(200)]
        [Column("website")]
        public string? website { get; set; }

        [MaxLength(100)]
        [Column("contact")]
        public string? contact { get; set; }

        public ICollection<InstituteCourse> Courses { get; set; } = new List<InstituteCourse>();
        public ICollection<InstituteLocation> Locations { get; set; } = new List<InstituteLocation>();
    }
}
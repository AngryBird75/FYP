using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspiraHub.Models
{
    [Table("Universities")]
    public class University
    {
        [Key]
        [Column("university_id")]
        public int university_id { get; set; }

        [Required, MaxLength(200)]
        [Column("name")]
        public string name { get; set; } = "";

        [MaxLength(200)]
        [Column("location")]
        public string? location { get; set; }

        [Column("ranking")]
        public int? ranking { get; set; }

        [MaxLength(50)]
        [Column("type")]
        public string? type { get; set; }

        [Column("programs")]
        public string? programs { get; set; }

        [MaxLength(200)]
        [Column("website")]
        public string? website { get; set; }

        [Column("description")]
        public string? description { get; set; }

        [MaxLength(100)]
        [Column("fee_structure")]
        public string? fee_structure { get; set; }
    }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspiraHub.Models
{
    [Table("Skills")]
    public class Skill
    {
        [Key]
        [Column("skill_id")]
        public int skill_id { get; set; }

        [Required, MaxLength(100)]
        [Column("skill_name")]
        public string skill_name { get; set; } = "";

        [MaxLength(100)]
        [Column("category")]
        public string? category { get; set; }
    }
}
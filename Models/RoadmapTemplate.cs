using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspiraHub.Models
{
    [Table("RoadmapTemplates")]
    public class RoadmapTemplate
    {
        [Key]
        [Column("template_id")]
        public int template_id { get; set; }

        [Column("career_id")]
        public int? career_id { get; set; }

        [Required, MaxLength(200)]
        [Column("title")]
        public string title { get; set; } = "";

        [Column("description")]
        public string? description { get; set; }

        [Column("is_active")]
        public bool is_active { get; set; } = true;

        [Column("created_at")]
        public DateTime created_at { get; set; } = DateTime.Now;

        [MaxLength(50)]
        [Column("education_level")]
        public string? education_level { get; set; }

        public ICollection<RoadmapTemplateStep> Steps { get; set; } = new List<RoadmapTemplateStep>();
    }
}
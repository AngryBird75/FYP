using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspiraHub.Models
{
    [Table("RoadmapTemplateSteps")]
    public class RoadmapTemplateStep
    {
        [Key]
        [Column("step_id")]
        public int step_id { get; set; }

        [Column("template_id")]
        public int template_id { get; set; }

        [Required, MaxLength(200)]
        [Column("step_title")]
        public string step_title { get; set; } = "";

        [Column("description")]
        public string? description { get; set; }

        [Column("step_order")]
        public int step_order { get; set; }

        [MaxLength(300)]
        [Column("resource_url")]
        public string? resource_url { get; set; }

        [ForeignKey("template_id")]
        public RoadmapTemplate Template { get; set; }
    }
}
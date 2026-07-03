using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspiraHub.Models
{
    [Table("RoadmapSteps")]
    public class RoadmapStep
    {
        [Key]
        [Column("step_id")]
        public int step_id { get; set; }

        [Column("roadmap_id")]
        public int roadmap_id { get; set; }

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

        [ForeignKey("roadmap_id")]
        public Roadmap Roadmap { get; set; }
    }
}
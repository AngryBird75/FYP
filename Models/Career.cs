using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspiraHub.Models
{
    [Table("Careers")]
    public class Career
    {
        [Key]
        [Column("career_id")]
        public int career_id { get; set; }

        [Required, MaxLength(200)]
        [Column("title")]
        public string title { get; set; } = "";

        [Column("description")]
        public string? description { get; set; }

        [Column("average_salary")]
        public int? average_salary { get; set; }

        [MaxLength(100)]
        [Column("scope")]
        public string? scope { get; set; }

        [MaxLength(50)]
        [Column("demand_level")]
        public string? demand_level { get; set; }

        [MaxLength(50)]
        [Column("job_market_trend")]
        public string? job_market_trend { get; set; }
    }
}
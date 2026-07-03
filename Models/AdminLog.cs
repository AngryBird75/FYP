using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspiraHub.Models
{
    [Table("AdminLogs")]
    public class AdminLog
    {
        [Key]
        [Column("log_id")]
        public int log_id { get; set; }

        [Column("admin_id")]
        public int admin_id { get; set; }

        [Required, MaxLength(200)]
        [Column("action")]
        public string action { get; set; } = "";

        [MaxLength(100)]
        [Column("target_table")]
        public string? target_table { get; set; }

        [Column("target_id")]
        public int? target_id { get; set; }

        [Column("details")]
        public string? details { get; set; }

        [MaxLength(50)]
        [Column("ip_address")]
        public string? ip_address { get; set; }

        [Column("created_at")]
        public DateTime created_at { get; set; } = DateTime.Now;
    }
}
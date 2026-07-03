using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspiraHub.Models
{
    [Table("Announcements")]
    public class Announcement
    {
        [Key]
        [Column("announcement_id")]
        public int announcement_id { get; set; }

        [Column("admin_id")]
        public int admin_id { get; set; }

        [Required, MaxLength(200)]
        [Column("title")]
        public string title { get; set; } = "";

        [Required]
        [Column("content")]
        public string content { get; set; } = "";

        [MaxLength(20)]
        [Column("target_role")]
        public string? target_role { get; set; }

        [Column("is_active")]
        public bool is_active { get; set; } = true;

        [Column("published_at")]
        public DateTime published_at { get; set; } = DateTime.Now;

        [Column("expires_at")]
        public DateTime? expires_at { get; set; }
    }
}
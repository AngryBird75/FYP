using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspiraHub.Models
{
    [Table("Notifications")]
    public class Notification
    {
        [Key]
        [Column("notif_id")]
        public int notif_id { get; set; }

        [Column("user_id")]
        public int user_id { get; set; }

        [Required, MaxLength(50)]
        [Column("type")]
        public string type { get; set; } = "";

        [Required, MaxLength(200)]
        [Column("title")]
        public string title { get; set; } = "";

        [Column("body")]
        public string? body { get; set; }

        [Column("ref_id")]
        public int? ref_id { get; set; }

        [Column("is_read")]
        public bool is_read { get; set; } = false;

        [Column("created_at")]
        public DateTime created_at { get; set; } = DateTime.Now;

        [ForeignKey("user_id")]
        public User User { get; set; }
    }
}
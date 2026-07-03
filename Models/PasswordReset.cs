using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspiraHub.Models
{
    [Table("PasswordResets")]
    public class PasswordReset
    {
        [Key]
        [Column("id")]
        public int id { get; set; }

        [Column("user_id")]
        public int user_id { get; set; }

        [Required, MaxLength(10)]
        [Column("otp_code")]
        public string otp_code { get; set; } = "";

        [Column("expires_at")]
        public DateTime expires_at { get; set; }

        [Column("used")]
        public bool used { get; set; } = false;

        [Column("created_at")]
        public DateTime created_at { get; set; } = DateTime.Now;

        [ForeignKey("user_id")]
        public User? User { get; set; }
    }
}

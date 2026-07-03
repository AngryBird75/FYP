using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspiraHub.Models
{
    [Table("Users")]
    public class User
    {
        [Key]
        [Column("user_id")]
        public int user_id { get; set; }

        [Required, MaxLength(100)]
        [Column("name")]
        public string name { get; set; }

        [Required, MaxLength(100)]
        [Column("email")]
        public string email { get; set; }

        [Required, MaxLength(255)]
        [Column("hashed_password")]
        public string hashed_password { get; set; }

        [Required, MaxLength(20)]
        [Column("role")]
        public string role { get; set; }

        [MaxLength(300)]
        [Column("profile_picture")]
        public string? profile_picture { get; set; }

        [Column("is_active")]
        public bool is_active { get; set; } = true;

        [Column("created_at")]
        public DateTime created_at { get; set; } = DateTime.Now;

        // ✅ DB mein column exist karta hai
        [Required, MaxLength(20)]
        [Column("unique_key")]
        public string unique_key { get; set; } = string.Empty;

        // Navigation
        public StudentProfile? StudentProfile { get; set; }
        public CompanyProfile? CompanyProfile { get; set; }
    }
}
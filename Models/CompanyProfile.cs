using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspiraHub.Models
{
    [Table("CompanyProfiles")]
    public class CompanyProfile
    {
        [Key]
        [Column("company_id")]
        public int company_id { get; set; }

        [Column("user_id")]
        public int user_id { get; set; }

        [Required, MaxLength(100)]
        [Column("company_name")]
        public string company_name { get; set; }

        [MaxLength(100)]
        [Column("industry")]
        public string? industry { get; set; }

        [MaxLength(100)]
        [Column("location")]
        public string? location { get; set; }

        [Column("description")]
        public string? description { get; set; }

        [MaxLength(200)]
        [Column("website")]
        public string? website { get; set; }

        [MaxLength(300)]
        [Column("logo_url")]
        public string? logo_url { get; set; }

        [MaxLength(50)]
        [Column("employee_count")]
        public string? employee_count { get; set; }

        [Column("is_verified")]
        public bool is_verified { get; set; } = false;

        [Column("founded_year")]
        public int? founded_year { get; set; }

        // Navigation
        [ForeignKey("user_id")]
        public User User { get; set; }
    }
}
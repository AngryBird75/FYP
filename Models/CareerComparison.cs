using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspiraHub.Models
{
    [Table("CareerComparisons")]
    public class CareerComparison
    {
        [Key]
        [Column("id")]
        public int id { get; set; }

        [Column("student_id")]
        public int student_id { get; set; }

        [Column("career_id_1")]
        public int career_id_1 { get; set; }

        [Column("career_id_2")]
        public int career_id_2 { get; set; }

        [Column("compared_at")]
        public DateTime compared_at { get; set; } = DateTime.Now;
    }
}
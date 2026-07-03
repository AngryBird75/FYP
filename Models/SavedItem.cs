using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspiraHub.Models
{
    [Table("SavedItems")]
    public class SavedItem
    {
        [Key]
        [Column("id")]
        public int id { get; set; }

        [Column("user_id")]
        public int user_id { get; set; }

        [Required, MaxLength(20)]
        [Column("item_type")]
        public string item_type { get; set; } = "Career";

        [Column("item_id")]
        public int item_id { get; set; }

        [Column("saved_at")]
        public DateTime saved_at { get; set; } = DateTime.Now;
    }
}
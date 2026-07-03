using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspiraHub.Models
{
    [Table("InstituteLocations")]
    public class InstituteLocation
    {
        [Key]
        [Column("location_id")]
        public int location_id { get; set; }

        [Column("institute_id")]
        public int institute_id { get; set; }

        [Column("latitude")]
        public double? latitude { get; set; }

        [Column("longitude")]
        public double? longitude { get; set; }

        [ForeignKey("institute_id")]
        public Institute? Institute { get; set; }
    }
}
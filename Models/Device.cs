using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Winedge.Models
{
    public class Device
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string DeviceName { get; set; }

        [Column(TypeName = "decimal(10,7)")]
        public decimal Latitude { get; set; }

        [Column(TypeName = "decimal(10,7)")]
        public decimal Longitude { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Winedge.Models
{
    public class Device
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string DeviceName { get; set; }

        [Column(TypeName = "decimal(10,7)")]
        public decimal Latitude { get; set; }

        [Column(TypeName = "decimal(10,7)")]
        public decimal Longitude { get; set; }

        public int? TemperatureLimit { get; set; }
        public int? HumidityLimit { get; set; }
        public int? LuminosityLimit { get; set; }
    }
}

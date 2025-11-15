using System.ComponentModel.DataAnnotations;

namespace Winedge.Models
{
    public class UserDevice
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string User { get; set; }

        [Required]
        public string Device { get; set; }
    }
}

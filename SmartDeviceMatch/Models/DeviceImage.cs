using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartDeviceMatch.Models
{
    public class DeviceImage
    {
        
        [Key]
        public int Id { get; set; }

        [Required]
        public required string DeviceId { get; set; }
        [ForeignKey("DeviceId")]
        public Device? Device { get; set; }

        [Required]
        public required string ImageUrl { get; set; }

        public int DisplayOrder { get; set; } = 0;
        public bool IsPrimary { get; set; } = false;
    }
}
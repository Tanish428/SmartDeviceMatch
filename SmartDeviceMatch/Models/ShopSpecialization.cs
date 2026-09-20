using System.ComponentModel.DataAnnotations;

namespace SmartDeviceMatch.Models
{
    public class ShopSpecialization
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ShopId { get; set; }
        public RepairShop? Shop { get; set; }

        [Required]
        public int CategoryId { get; set; } 
        public DeviceCategory? Category { get; set; }

        [StringLength(50)]
        public string? BrandName { get; set; } 

        [Range(1, 5)] // Ensures rating is between 1 (Novice) and 5 (Expert)
        public int ExpertiseLevel { get; set; }

        [StringLength(250)]
        public string? Description { get; set; } 
    }
}
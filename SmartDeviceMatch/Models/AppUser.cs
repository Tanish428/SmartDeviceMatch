using System.ComponentModel.DataAnnotations;

namespace SmartDeviceMatch.Models
{
    public class AppUser
    {
        // 2. ADDED: Standard Primary Key for your dedicated AppUser table
        [Key]
        public int Id { get; set; }

        // 3. ADDED: The 1-to-1 link back to Microsoft's AspNetUsers table
        [Required]
        public required string IdentityUserId { get; set; }

        

        [Required, StringLength(100)]
        public required string FullName { get; set; }

        public string? ProfileImageUrl { get; set; }

        [Required, StringLength(20)]
        public required string UserType { get; set; } // "DeviceOwner", "RepairShop", "Buyer", "Admin"

        [StringLength(50)]
        public string? City { get; set; }

        [StringLength(50)]
        public string? State { get; set; }

        [StringLength(10)]
        public string? PinCode { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public double Rating { get; set; } = 0.0;
        public bool IsVerified { get; set; } = false;
        public bool IsBanned { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
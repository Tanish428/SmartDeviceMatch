using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace SmartDeviceMatch.Models
{
    public class AppUser : IdentityUser
    {
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
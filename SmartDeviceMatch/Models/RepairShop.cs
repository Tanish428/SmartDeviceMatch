using System.ComponentModel.DataAnnotations;

namespace SmartDeviceMatch.Models
{
    public class RepairShop
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public required string UserId { get; set; }
        public AppUser? User { get; set; }

        [Required, StringLength(100)]
        public required string ShopName { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(50)]
        public string? LicenseNumber { get; set; } 

        public bool IsVerified { get; set; } = false; 
        public int YearsOfExperience { get; set; }

        [StringLength(100)]
        public string? OperatingHours { get; set; } 

        [Required, StringLength(50)]
        public required string City { get; set; }

        [Required, StringLength(50)]
        public required string State { get; set; }

        [Required, StringLength(10)]
        public required string PinCode { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public double Rating { get; set; } = 0.0;
        public int TotalReviews { get; set; } = 0;
    }
}
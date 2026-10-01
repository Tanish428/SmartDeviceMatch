using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartDeviceMatch.Models
{
    public class Match
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DeviceId { get; set; }

        [ForeignKey(nameof(DeviceId))]
        public Device? Device { get; set; }

        [Required]
        public int ShopId { get; set; }

        [ForeignKey(nameof(ShopId))]
        public RepairShop? Shop { get; set; }

        [Range(0, 100)]
        public int CompatibilityScore { get; set; }

        [Range(0, 30)]
        public int CategoryScore { get; set; }

        [Range(0, 25)]
        public int BrandScore { get; set; }

        [Range(0, 20)]
        public int ProximityScore { get; set; }

        [Range(0, 15)]
        public int ExpertiseScore { get; set; }

        [Range(0, 10)]
        public int RatingScore { get; set; }

        public double DistanceKm { get; set; }

        [Required, StringLength(20)]
        public string Status { get; set; } = "Active";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
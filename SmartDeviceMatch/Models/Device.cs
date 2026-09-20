using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartDeviceMatch.Models
{
    public class Device
    {
        [Key]
        public int Id { get; set; } 

        [Required]
        public required string OwnerId { get; set; }
        [ForeignKey("OwnerId")]
        public AppUser? Owner { get; set; }

        [Required]
        public int CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public DeviceCategory? Category { get; set; }

        [Required, StringLength(100)]
        public required string BrandName { get; set; }

        [Required, StringLength(100)]
        public required string ModelName { get; set; }

        [Required, StringLength(10)]
        public required string ConditionGrade { get; set; } // A, B, C, D, E, F

        [Required, StringLength(1000)]
        public required string ConditionDescription { get; set; }

        [Required, StringLength(100)]
        public required string DamageType { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal AskingPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? EstimatedValue { get; set; }

        [Required, StringLength(20)]
        public string Status { get; set; } = "Listed"; // Listed, Sold, Withdrawn

        [Required, StringLength(50)]
        public required string City { get; set; }

        [Required, StringLength(50)]
        public required string State { get; set; }

        [Required, StringLength(10)]
        public required string PinCode { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public int ViewCount { get; set; } = 0;
        public bool IsUrgent { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;

        public ICollection<DeviceImage>? Images { get; set; }
    }
}
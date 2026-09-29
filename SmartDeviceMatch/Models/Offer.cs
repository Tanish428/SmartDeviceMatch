using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartDeviceMatch.Models
{
    public class Offer
    {
        [Key]
        public int Id { get; set; }

        // Device involved in the offer
        [Required]
        public int DeviceId { get; set; }

        public Device? Device { get; set; }


        // Buyer who makes a purchase offer
        // Null for Repair offers
        public int? BuyerId { get; set; }

        [ForeignKey(nameof(BuyerId))]
        public AppUser? Buyer { get; set; }


        // Repair shop involved in the offer
        // Used for Repair offers and Buy-offer seller
        public int? RepairShopId { get; set; }

        [ForeignKey(nameof(RepairShopId))]
        public RepairShop? RepairShop { get; set; }


        // Amount offered
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal OfferAmount { get; set; }


        // Counter offer amount
        [Column(TypeName = "decimal(18,2)")]
        public decimal? CounterOfferAmount { get; set; }


        [Required, StringLength(3)]
        public string Currency { get; set; } = "INR";


        // Optional message from the person making the offer
        [StringLength(1000)]
        public string? Message { get; set; }


        // Repair / Buy / Scrap
        [Required, StringLength(20)]
        public required string OfferType { get; set; }


        // Pending / Accepted / Rejected / Completed
        [Required, StringLength(20)]
        public string Status { get; set; } = "Pending";


        // Offer expiration
        public DateTime ExpiresAt { get; set; }


        // Payment / transaction information for later phases
        public DateTime? EscrowedAt { get; set; }

        public DateTime? VerifiedAt { get; set; }

        public DateTime? ReleasedAt { get; set; }
    }
}
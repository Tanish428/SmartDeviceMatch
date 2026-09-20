using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartDeviceMatch.Models
{
    public class Offer
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DeviceId { get; set; }
        public Device? Device { get; set; }

        [Required]
        public required string BuyerId { get; set; } 
        public AppUser? Buyer { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")] 
        public decimal OfferAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? CounterOfferAmount { get; set; }

        [Required, StringLength(3)]
        public string Currency { get; set; } = "INR"; 

        [StringLength(1000)]
        public string? Message { get; set; } 

        [Required, StringLength(20)]
        public required string OfferType { get; set; } // "Repair", "Buy", "Scrap"

        [Required, StringLength(20)]
        public string Status { get; set; } = "Pending"; // "Pending", "Accepted", "Escrowed", "Released"

        public DateTime ExpiresAt { get; set; } // Mandatory deadline for the offer

        // Optional timestamps for the Transaction/Escrow states
        public DateTime? EscrowedAt { get; set; }
        public DateTime? VerifiedAt { get; set; } // When inspection is successful
        public DateTime? ReleasedAt { get; set; } // Final payment release
    }
}
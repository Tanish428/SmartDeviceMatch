using System.ComponentModel.DataAnnotations;

namespace SmartDeviceMatch.Models
{
    public class Dispute
    {
        public int Id { get; set; }

        [Required]
        public int OfferId { get; set; }

        public Offer? Offer { get; set; }

        [Required]
        public int RaisedByUserId { get; set; }

        public AppUser? RaisedByUser { get; set; }

        [Required]
        [StringLength(1000)]
        public string Reason { get; set; } = string.Empty;

        // Open / UnderReview / Resolved / Rejected
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Open";

        [StringLength(1500)]
        public string? AdminResolution { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ResolvedAt { get; set; }
    }
}
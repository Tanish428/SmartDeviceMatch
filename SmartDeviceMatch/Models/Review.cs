using System.ComponentModel.DataAnnotations;

namespace SmartDeviceMatch.Models
{
    public class Review
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OfferId { get; set; }
        public Offer? Offer { get; set; }

        [Required]
        public required string ReviewerId { get; set; } 
        public AppUser? Reviewer { get; set; }

        [Required]
        public required string RevieweeId { get; set; }
        public AppUser? Reviewee { get; set; }

        [Required, Range(1, 5)] 
        public int Rating { get; set; } // 1 to 5 Stars

        [StringLength(500)]
        public string? Comment { get; set; }

        public bool IsAnonymous { get; set; } = false; // Hide name but show rating
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
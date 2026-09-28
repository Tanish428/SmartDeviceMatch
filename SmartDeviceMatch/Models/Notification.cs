using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartDeviceMatch.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public AppUser? User { get; set; }

        [Required, StringLength(100)]
        public required string Title { get; set; } 

        [Required, StringLength(500)]
        public required string Message { get; set; } 

        [Required, StringLength(50)]
        public required string Type { get; set; } // "MatchAlert", "NewOffer", "EscrowAlert"

        
        [StringLength(100)]
        public string? ReferenceId { get; set; } 

        [StringLength(50)]
        public string? ReferenceType { get; set; }

        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
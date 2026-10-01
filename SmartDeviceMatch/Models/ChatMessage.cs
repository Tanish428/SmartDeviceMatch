using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartDeviceMatch.Models
{
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }

        // User who sent the message
        [Required]
        public int SenderId { get; set; }

        [ForeignKey(nameof(SenderId))]
        public AppUser? Sender { get; set; }


        // User who receives the message
        [Required]
        public int ReceiverId { get; set; }

        [ForeignKey(nameof(ReceiverId))]
        public AppUser? Receiver { get; set; }


        // Device related to this conversation
        [Required]
        public int DeviceId { get; set; }

        [ForeignKey(nameof(DeviceId))]
        public Device? Device { get; set; }


        // Message text
        [Required]
        [StringLength(1000)]
        public required string Content { get; set; }


        // Currently only Text is supported
        [Required]
        [StringLength(20)]
        public string MessageType { get; set; } = "Text";


        // Read status
        public bool IsRead { get; set; } = false;

        public DateTime? ReadAt { get; set; }


        // Message timestamp
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}
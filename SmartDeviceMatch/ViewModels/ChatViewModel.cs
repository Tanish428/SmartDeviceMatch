namespace SmartDeviceMatch.ViewModels
{
    public class ChatViewModel
    {
        public int DeviceId { get; set; }

        public int OtherUserId { get; set; }

        public int CurrentUserId { get; set; }

        public string DeviceName { get; set; } = string.Empty;

        public string OtherUserName { get; set; } = string.Empty;

        public List<ChatMessageViewModel> Messages { get; set; }
            = new List<ChatMessageViewModel>();
    }


    public class ChatMessageViewModel
    {
        public int Id { get; set; }

        public int SenderId { get; set; }

        public int ReceiverId { get; set; }

        public string Content { get; set; } = string.Empty;

        public bool IsRead { get; set; }

        public DateTime SentAt { get; set; }

        public bool IsMine { get; set; }
    }


    public class ChatConversationViewModel
    {
        public int DeviceId { get; set; }

        public int OtherUserId { get; set; }

        public string OtherUserName { get; set; } = string.Empty;

        public string DeviceName { get; set; } = string.Empty;

        public string? LastMessage { get; set; }

        public DateTime? LastMessageAt { get; set; }

        public int UnreadCount { get; set; }

        public int OfferId { get; set; }
    }
}
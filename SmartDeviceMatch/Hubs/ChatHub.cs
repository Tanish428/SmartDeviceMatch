using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SmartDeviceMatch.Data;
using SmartDeviceMatch.Services;

namespace SmartDeviceMatch.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;

        public ChatHub(
            ApplicationDbContext context,
            INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }


        // ==========================================
        // Join conversation
        // ==========================================

        public async Task JoinChat(
            int deviceId,
            int otherUserId)
        {
            var currentUserId =
                await GetCurrentAppUserId();

            if (currentUserId == null)
            {
                throw new HubException(
                    "User profile not found.");
            }

            var allowed =
                await CanUsersChat(
                    deviceId,
                    currentUserId.Value,
                    otherUserId);

            if (!allowed)
            {
                throw new HubException(
                    "You are not allowed to access this chat.");
            }

            var group =
                GetChatGroup(
                    deviceId,
                    currentUserId.Value,
                    otherUserId);

            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                group);
        }


        // ==========================================
        // Send message
        // ==========================================

        public async Task SendMessage(
            int deviceId,
            int receiverId,
            string content)
        {
            var senderId =
                await GetCurrentAppUserId();

            if (senderId == null)
            {
                throw new HubException(
                    "User profile not found.");
            }

            content = content?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(content))
            {
                return;
            }

            if (content.Length > 1000)
            {
                throw new HubException(
                    "Message cannot exceed 1000 characters.");
            }

            var allowed =
                await CanUsersChat(
                    deviceId,
                    senderId.Value,
                    receiverId);

            if (!allowed)
            {
                throw new HubException(
                    "You are not allowed to send messages in this chat.");
            }


            // ==========================================
            // Save message
            // ==========================================

            var message = new Models.ChatMessage
            {
                SenderId = senderId.Value,
                ReceiverId = receiverId,
                DeviceId = deviceId,
                Content = content,
                MessageType = "Text",
                IsRead = false,
                SentAt = DateTime.UtcNow
            };

            _context.ChatMessages.Add(message);

            await _context.SaveChangesAsync();


            // ==========================================
            // Send real-time message
            // ==========================================

            var group =
                GetChatGroup(
                    deviceId,
                    senderId.Value,
                    receiverId);

            await Clients.Group(group)
                .SendAsync(
                    "ReceiveMessage",
                    new
                    {
                        id = message.Id,
                        senderId = message.SenderId,
                        receiverId = message.ReceiverId,
                        content = message.Content,
                        sentAt = message.SentAt,
                        isRead = message.IsRead
                    });


            // ==========================================
            // Create notification
            // ==========================================

            await _notificationService.CreateAsync(
                receiverId,
                "New Message",
                content.Length > 100
                    ? content[..100] + "..."
                    : content,
                "ChatMessage",
                message.Id.ToString(),
                "ChatMessage");
        }


        // ==========================================
        // Mark messages as read
        // ==========================================

        public async Task MarkMessagesAsRead(
            int deviceId,
            int otherUserId)
        {
            var currentUserId =
                await GetCurrentAppUserId();

            if (currentUserId == null)
            {
                return;
            }

            var allowed =
                await CanUsersChat(
                    deviceId,
                    currentUserId.Value,
                    otherUserId);

            if (!allowed)
            {
                return;
            }

            var messages =
                await _context.ChatMessages
                    .Where(m =>
                        m.DeviceId == deviceId &&
                        m.SenderId == otherUserId &&
                        m.ReceiverId == currentUserId.Value &&
                        !m.IsRead)
                    .ToListAsync();

            if (!messages.Any())
            {
                return;
            }

            foreach (var message in messages)
            {
                message.IsRead = true;
                message.ReadAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();


            var group =
                GetChatGroup(
                    deviceId,
                    currentUserId.Value,
                    otherUserId);

            await Clients.Group(group)
                .SendAsync(
                    "MessagesRead",
                    currentUserId.Value,
                    otherUserId);
        }


        // ==========================================
        // Typing indicator
        // ==========================================

        public async Task SendTyping(
            int deviceId,
            int receiverId,
            bool isTyping)
        {
            var senderId =
                await GetCurrentAppUserId();

            if (senderId == null)
            {
                return;
            }

            var allowed =
                await CanUsersChat(
                    deviceId,
                    senderId.Value,
                    receiverId);

            if (!allowed)
            {
                return;
            }

            var group =
                GetChatGroup(
                    deviceId,
                    senderId.Value,
                    receiverId);

            await Clients.Group(group)
                .SendAsync(
                    "UserTyping",
                    senderId.Value,
                    isTyping);
        }


        // ==========================================
        // Helper: Current AppUser
        // ==========================================

        private async Task<int?> GetCurrentAppUserId()
        {
            var identityUserId =
                Context.UserIdentifier;

            if (string.IsNullOrEmpty(identityUserId))
            {
                return null;
            }

            var appUser =
                await _context.AppUsers
                    .FirstOrDefaultAsync(u =>
                        u.IdentityUserId == identityUserId);

            return appUser?.Id;
        }


        // ==========================================
        // Helper: Check chat permission
        // ==========================================

        private async Task<bool> CanUsersChat(
            int deviceId,
            int firstUserId,
            int secondUserId)
        {
            if (firstUserId == secondUserId)
            {
                return false;
            }

            var offers =
                await _context.Offers
                    .Include(o => o.Device)
                    .Include(o => o.RepairShop)
                    .Where(o =>
                        o.DeviceId == deviceId)
                    .ToListAsync();

            foreach (var offer in offers)
            {
                if (offer.Device == null)
                {
                    continue;
                }


                // ==========================================
                // Repair chat
                // DeviceOwner <-> RepairShop
                // ==========================================

                if (offer.OfferType == "Repair" &&
                    offer.RepairShop != null)
                {
                    var ownerId =
                        offer.Device.OwnerId;

                    var repairShopUserId =
                        offer.RepairShop.UserId;

                    if ((firstUserId == ownerId &&
                         secondUserId == repairShopUserId)
                        ||
                        (firstUserId == repairShopUserId &&
                         secondUserId == ownerId))
                    {
                        return true;
                    }
                }


                // ==========================================
                // Purchase chat
                // Buyer <-> RepairShop
                // ==========================================

                if (offer.OfferType == "Buy" &&
                    offer.BuyerId.HasValue &&
                    offer.RepairShop != null)
                {
                    var buyerId =
                        offer.BuyerId.Value;

                    var repairShopUserId =
                        offer.RepairShop.UserId;

                    if ((firstUserId == buyerId &&
                         secondUserId == repairShopUserId)
                        ||
                        (firstUserId == repairShopUserId &&
                         secondUserId == buyerId))
                    {
                        return true;
                    }
                }
            }

            return false;
        }


        // ==========================================
        // Chat group
        // ==========================================

        public static string GetChatGroup(
            int deviceId,
            int user1,
            int user2)
        {
            var smaller =
                Math.Min(user1, user2);

            var larger =
                Math.Max(user1, user2);

            return $"Chat_{deviceId}_{smaller}_{larger}";
        }
    }
}
using Microsoft.AspNetCore.SignalR;
using SmartDeviceMatch.Data;
using SmartDeviceMatch.Hubs;
using SmartDeviceMatch.Models;

namespace SmartDeviceMatch.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(
            ApplicationDbContext context,
            IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task CreateAsync(
            int userId,
            string title,
            string message,
            string type,
            string? referenceId = null,
            string? referenceType = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                ReferenceId = referenceId,
                ReferenceType = referenceType,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();

            var groupName =
                NotificationHub.GetGroupName(userId);

            await _hubContext.Clients
                .Group(groupName)
                .SendAsync(
                    "ReceiveNotification",
                    new
                    {
                        id = notification.Id,
                        title = notification.Title,
                        message = notification.Message,
                        type = notification.Type,
                        referenceId = notification.ReferenceId,
                        referenceType = notification.ReferenceType,
                        isRead = notification.IsRead,
                        createdAt = notification.CreatedAt
                    });
        }
    }
}
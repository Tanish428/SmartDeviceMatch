using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SmartDeviceMatch.Data;

namespace SmartDeviceMatch.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly ApplicationDbContext _context;

        public NotificationHub(ApplicationDbContext context)
        {
            _context = context;
        }

        public override async Task OnConnectedAsync()
        {
            var identityUserId = Context.UserIdentifier;

            if (!string.IsNullOrEmpty(identityUserId))
            {
                var appUser = await _context.AppUsers
                    .FirstOrDefaultAsync(u =>
                        u.IdentityUserId == identityUserId);

                if (appUser != null)
                {
                    var groupName = GetGroupName(appUser.Id);

                    await Groups.AddToGroupAsync(
                        Context.ConnectionId,
                        groupName);
                }
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(
            Exception? exception)
        {
            var identityUserId = Context.UserIdentifier;

            if (!string.IsNullOrEmpty(identityUserId))
            {
                var appUser = await _context.AppUsers
                    .FirstOrDefaultAsync(u =>
                        u.IdentityUserId == identityUserId);

                if (appUser != null)
                {
                    var groupName = GetGroupName(appUser.Id);

                    await Groups.RemoveFromGroupAsync(
                        Context.ConnectionId,
                        groupName);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        public static string GetGroupName(int appUserId)
        {
            return $"AppUser_{appUserId}";
        }
    }
}
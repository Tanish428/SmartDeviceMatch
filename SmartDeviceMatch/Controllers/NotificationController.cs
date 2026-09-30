using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartDeviceMatch.Data;

namespace SmartDeviceMatch.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public NotificationController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ==========================================
        // GET: Notification/Index
        // ==========================================

        public async Task<IActionResult> Index()
        {
            var appUserId = await GetCurrentAppUserId();

            if (appUserId == null)
            {
                return RedirectToAction(
                    "Create",
                    "Profile");
            }

            var notifications = await _context.Notifications
                .Where(n => n.UserId == appUserId.Value)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return View(notifications);
        }


        // ==========================================
        // GET: Notification/Recent
        // Used by notification dropdown
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Recent()
        {
            var appUserId = await GetCurrentAppUserId();

            if (appUserId == null)
            {
                return Unauthorized();
            }

            var notifications = await _context.Notifications
                .Where(n => n.UserId == appUserId.Value)
                .OrderByDescending(n => n.CreatedAt)
                .Take(10)
                .Select(n => new
                {
                    id = n.Id,
                    title = n.Title,
                    message = n.Message,
                    type = n.Type,
                    isRead = n.IsRead,
                    createdAt = n.CreatedAt
                })
                .ToListAsync();

            return Json(notifications);
        }


        // ==========================================
        // GET: Notification/UnreadCount
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            var appUserId = await GetCurrentAppUserId();

            if (appUserId == null)
            {
                return Unauthorized();
            }

            var count = await _context.Notifications
                .CountAsync(n =>
                    n.UserId == appUserId.Value &&
                    !n.IsRead);

            return Json(new
            {
                count
            });
        }


        // ==========================================
        // POST: Notification/MarkAsRead
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var appUserId = await GetCurrentAppUserId();

            if (appUserId == null)
            {
                return Unauthorized();
            }

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n =>
                    n.Id == id &&
                    n.UserId == appUserId.Value);

            if (notification == null)
            {
                return NotFound();
            }

            notification.IsRead = true;

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true
            });
        }


        // ==========================================
        // POST: Notification/MarkAllAsRead
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var appUserId = await GetCurrentAppUserId();

            if (appUserId == null)
            {
                return Unauthorized();
            }

            var notifications = await _context.Notifications
                .Where(n =>
                    n.UserId == appUserId.Value &&
                    !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true
            });
        }


        // ==========================================
        // Helper
        // ==========================================

        private async Task<int?> GetCurrentAppUserId()
        {
            var identityUserId =
                _userManager.GetUserId(User);

            if (identityUserId == null)
            {
                return null;
            }

            var appUser = await _context.AppUsers
                .FirstOrDefaultAsync(u =>
                    u.IdentityUserId == identityUserId);

            return appUser?.Id;
        }
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartDeviceMatch.Data;
using SmartDeviceMatch.Models;
using SmartDeviceMatch.ViewModels;

namespace SmartDeviceMatch.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public AdminController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        // ============================================================
        // ADMIN DASHBOARD
        // ============================================================


        public async Task<IActionResult> Index()
        {
            var now = DateTime.UtcNow;

            // First month of the six-month reporting period
            var firstMonth = new DateTime(now.Year, now.Month, 1)
                .AddMonths(-5);

            var model = new AdminDashboardViewModel
            {
                // ==========================================
                // USER STATISTICS
                // ==========================================

                TotalUsers = await _context.AppUsers.CountAsync(),

                DeviceOwners = await _context.AppUsers
                    .CountAsync(u => u.UserType == "DeviceOwner"),

                RepairShopUsers = await _context.AppUsers
                    .CountAsync(u => u.UserType == "RepairShop"),

                Buyers = await _context.AppUsers
                    .CountAsync(u => u.UserType == "Buyer"),

                BannedUsers = await _context.AppUsers
                    .CountAsync(u => u.IsBanned),

                // ==========================================
                // REPAIR SHOP STATISTICS
                // ==========================================

                TotalRepairShops = await _context.RepairShops.CountAsync(),

                VerifiedRepairShops = await _context.RepairShops
                    .CountAsync(r => r.IsVerified),

                PendingRepairShops = await _context.RepairShops
                    .CountAsync(r => !r.IsVerified),

                // ==========================================
                // DEVICE STATISTICS
                // ==========================================

                TotalDevices = await _context.Devices
                    .CountAsync(),

                ActiveListings = await _context.Devices
                    .CountAsync(d => !d.IsDeleted && d.Status == "Listed"),

                DeletedDevices = await _context.Devices
                    .CountAsync(d => d.IsDeleted),

                UrgentListings = await _context.Devices
                    .CountAsync(d => !d.IsDeleted && d.IsUrgent),

                DevicesInRepair = await _context.Devices
                    .CountAsync(d => !d.IsDeleted && d.Status == "Repairing"),

                RefurbishedDevices = await _context.Devices
                    .CountAsync(d => !d.IsDeleted && d.Status == "Refurbished"),

                // ==========================================
                // OFFER STATISTICS
                // ==========================================

                TotalOffers = await _context.Offers.CountAsync(),

                PendingOffers = await _context.Offers
                    .CountAsync(o => o.Status == "Pending"),

                AcceptedOffers = await _context.Offers
                    .CountAsync(o => o.Status == "Accepted"),

                // ==========================================
                // TRANSACTION STATISTICS
                // ==========================================

                CompletedTransactions = await _context.Offers
                    .CountAsync(o =>
                        o.OfferType == "Buy" &&
                        o.Status == "Completed"),

                EscrowedTransactions = await _context.Offers
                    .CountAsync(o =>
                        o.OfferType == "Buy" &&
                        o.Status == "Escrowed"),

                // ==========================================
                // MATCH STATISTICS
                // ==========================================

                TotalMatches = await _context.Matches.CountAsync(),

                ActiveMatches = await _context.Matches
                    .CountAsync(m => m.Status == "Active"),

                // ==========================================
                // REVIEW STATISTICS
                // ==========================================

                TotalReviews = await _context.Reviews.CountAsync(),

                AverageRating = await _context.Reviews.AnyAsync()
                    ? await _context.Reviews
                        .AverageAsync(r => (double)r.Rating)
                    : 0.0,

                // ==========================================
                // RECENT USERS
                // ==========================================

                RecentUsers = await _context.AppUsers
                    .AsNoTracking()
                    .OrderByDescending(u => u.CreatedAt)
                    .Take(8)
                    .ToListAsync(),

                // ==========================================
                // RECENT OFFERS
                // ==========================================

                RecentOffers = await _context.Offers
                    .AsNoTracking()
                    .Include(o => o.Device)
                    .Include(o => o.Buyer)
                    .Include(o => o.RepairShop)
                        .ThenInclude(r => r!.User)
                    .OrderByDescending(o => o.Id)
                    .Take(8)
                    .ToListAsync(),

                // ==========================================
                // RECENT REVIEWS
                // ==========================================

                RecentReviews = await _context.Reviews
                    .AsNoTracking()
                    .Include(r => r.Reviewer)
                    .Include(r => r.Reviewee)
                    .Include(r => r.Offer)
                        .ThenInclude(o => o!.Device)
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(8)
                    .ToListAsync()
            };

            // ==========================================
            // SIX-MONTH LISTING TREND
            // ==========================================

            var listingDates = await _context.Devices
                .AsNoTracking()
                .Where(d =>
                    !d.IsDeleted &&
                    d.CreatedAt >= firstMonth &&
                    d.CreatedAt < now)
                .Select(d => d.CreatedAt)
                .ToListAsync();

            for (int i = 0; i < 6; i++)
            {
                var monthStart = firstMonth.AddMonths(i);
                var monthEnd = monthStart.AddMonths(1);

                model.MonthlyListings.Add(new MonthlyCountViewModel
                {
                    Month = monthStart.ToString("MMM yyyy"),

                    Count = listingDates.Count(date =>
                        date >= monthStart &&
                        date < monthEnd)
                });
            }

            // ==========================================
            // DEVICE STATUS BREAKDOWN
            // ==========================================

            model.DeviceStatuses = await _context.Devices
                .AsNoTracking()
                .Where(d => !d.IsDeleted)
                .GroupBy(d => d.Status)
                .Select(group => new StatusCountViewModel
                {
                    Status = group.Key,
                    Count = group.Count()
                })
                .OrderByDescending(item => item.Count)
                .ToListAsync();

            return View(model);
        }


        // ============================================================
        // VIEW ALL USERS
        // ============================================================

        public async Task<IActionResult> Users()
        {
            var users = await _context.AppUsers
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return View(users);
        }


        // ============================================================
        // BAN USER
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BanUser(int id)
        {
            var user = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            // Do not allow admin account to be banned
            if (user.UserType == "Admin")
            {
                TempData["AdminMessage"] =
                    "Admin accounts cannot be banned.";

                return RedirectToAction(nameof(Index));
            }

            user.IsBanned = true;

            await _context.SaveChangesAsync();


            // Disable Identity user immediately if possible
            if (!string.IsNullOrEmpty(user.IdentityUserId))
            {
                var identityUser =
                    await _userManager.FindByIdAsync(
                        user.IdentityUserId);

                if (identityUser != null)
                {
                    await _userManager.SetLockoutEndDateAsync(
                        identityUser,
                        DateTimeOffset.UtcNow.AddYears(100));
                }
            }


            TempData["AdminMessage"] =
                "User has been banned successfully.";

            return RedirectToAction(nameof(Index));
        }


        // ============================================================
        // UNBAN USER
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnbanUser(int id)
        {
            var user = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            user.IsBanned = false;

            await _context.SaveChangesAsync();


            // Remove Identity lockout
            if (!string.IsNullOrEmpty(user.IdentityUserId))
            {
                var identityUser =
                    await _userManager.FindByIdAsync(
                        user.IdentityUserId);

                if (identityUser != null)
                {
                    await _userManager.SetLockoutEndDateAsync(
                        identityUser,
                        null);
                }
            }


            TempData["AdminMessage"] =
                "User has been unbanned successfully.";

            return RedirectToAction(nameof(Index));
        }


        // ============================================================
        // VERIFY REPAIR SHOP
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyRepairShop(int id)
        {
            var repairShop = await _context.RepairShops
                .FirstOrDefaultAsync(r => r.Id == id);

            if (repairShop == null)
            {
                return NotFound();
            }

            repairShop.IsVerified = true;

            await _context.SaveChangesAsync();

            TempData["AdminMessage"] =
                "Repair shop verified successfully.";

            return RedirectToAction(nameof(Index));
        }


        // ============================================================
        // UNVERIFY REPAIR SHOP
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnverifyRepairShop(int id)
        {
            var repairShop = await _context.RepairShops
                .FirstOrDefaultAsync(r => r.Id == id);

            if (repairShop == null)
            {
                return NotFound();
            }

            repairShop.IsVerified = false;

            await _context.SaveChangesAsync();

            TempData["AdminMessage"] =
                "Repair shop verification removed.";

            return RedirectToAction(nameof(Index));
        }


        // ============================================================
        // ALL DEVICES
        // ============================================================

        public async Task<IActionResult> Devices()
        {
            var devices = await _context.Devices
                .Include(d => d.Category)
                .OrderByDescending(d => d.Id)
                .ToListAsync();

            return View(devices);
        }


        // ============================================================
        // ALL OFFERS / TRANSACTIONS
        // ============================================================

        public async Task<IActionResult> Offers()
        {
            var offers = await _context.Offers
                .Include(o => o.Device)
                .Include(o => o.Buyer)
                .Include(o => o.RepairShop)
                    .ThenInclude(r => r!.User)
                .OrderByDescending(o => o.Id)
                .ToListAsync();

            return View(offers);
        }


        // ============================================================
        // ALL REVIEWS
        // ============================================================

        public async Task<IActionResult> Reviews()
        {
            var reviews = await _context.Reviews
                .Include(r => r.Reviewer)
                .Include(r => r.Reviewee)
                .Include(r => r.Offer)
                    .ThenInclude(o => o!.Device)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(reviews);
        }

        public async Task<IActionResult> RepairShops()
        {
            var shops = await _context.RepairShops
                .Include(r => r.User)
                .OrderBy(r => r.IsVerified)
                .ThenBy(r => r.ShopName)
                .ToListAsync();

            return View(shops);
        }
    }
}
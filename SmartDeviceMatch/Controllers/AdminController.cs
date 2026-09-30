using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartDeviceMatch.Data;
using SmartDeviceMatch.Models;

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
            // --------------------------------------------------------
            // User statistics
            // --------------------------------------------------------

            var totalUsers = await _context.AppUsers.CountAsync();

            var deviceOwners = await _context.AppUsers
                .CountAsync(u => u.UserType == "DeviceOwner");

            var repairShops = await _context.AppUsers
                .CountAsync(u => u.UserType == "RepairShop");

            var buyers = await _context.AppUsers
                .CountAsync(u => u.UserType == "Buyer");

            var bannedUsers = await _context.AppUsers
                .CountAsync(u => u.IsBanned);


            // --------------------------------------------------------
            // Device statistics
            // --------------------------------------------------------

            var totalDevices = await _context.Devices
                .CountAsync();

            var devicesInRepair = await _context.Devices
                .CountAsync(d => d.Status == "Repairing");

            var refurbishedDevices = await _context.Devices
                .CountAsync(d => d.Status == "Refurbished");


            // --------------------------------------------------------
            // Offer / transaction statistics
            // --------------------------------------------------------

            var totalOffers = await _context.Offers
                .CountAsync();

            var pendingOffers = await _context.Offers
                .CountAsync(o => o.Status == "Pending");

            var acceptedOffers = await _context.Offers
                .CountAsync(o => o.Status == "Accepted");

            var completedTransactions = await _context.Offers
                .CountAsync(o =>
                    o.OfferType == "Buy" &&
                    o.Status == "Completed");

            var escrowedTransactions = await _context.Offers
                .CountAsync(o =>
                    o.OfferType == "Buy" &&
                    o.Status == "Escrowed");


            // --------------------------------------------------------
            // Review statistics
            // --------------------------------------------------------

            var totalReviews = await _context.Reviews
                .CountAsync();

            double averageRating = totalReviews > 0
                ? await _context.Reviews
                    .AverageAsync(r => (double)r.Rating)
                : 0;


            // --------------------------------------------------------
            // Recent users
            // --------------------------------------------------------

            var recentUsers = await _context.AppUsers
                .OrderByDescending(u => u.CreatedAt)
                .Take(10)
                .ToListAsync();


            // --------------------------------------------------------
            // Recent offers
            // --------------------------------------------------------

            var recentOffers = await _context.Offers
                .Include(o => o.Device)
                .Include(o => o.Buyer)
                .Include(o => o.RepairShop)
                    .ThenInclude(r => r!.User)
                .OrderByDescending(o => o.Id)
                .Take(10)
                .ToListAsync();


            // --------------------------------------------------------
            // Recent reviews
            // --------------------------------------------------------

            var recentReviews = await _context.Reviews
                .Include(r => r.Reviewer)
                .Include(r => r.Reviewee)
                .Include(r => r.Offer)
                    .ThenInclude(o => o!.Device)
                .OrderByDescending(r => r.CreatedAt)
                .Take(10)
                .ToListAsync();


            // --------------------------------------------------------
            // Dashboard ViewBag
            // --------------------------------------------------------

            ViewBag.TotalUsers = totalUsers;
            ViewBag.DeviceOwners = deviceOwners;
            ViewBag.RepairShops = repairShops;
            ViewBag.Buyers = buyers;
            ViewBag.BannedUsers = bannedUsers;

            ViewBag.TotalDevices = totalDevices;
            ViewBag.DevicesInRepair = devicesInRepair;
            ViewBag.RefurbishedDevices = refurbishedDevices;

            ViewBag.TotalOffers = totalOffers;
            ViewBag.PendingOffers = pendingOffers;
            ViewBag.AcceptedOffers = acceptedOffers;
            ViewBag.CompletedTransactions = completedTransactions;
            ViewBag.EscrowedTransactions = escrowedTransactions;

            ViewBag.TotalReviews = totalReviews;
            ViewBag.AverageRating = averageRating;

            ViewBag.RecentUsers = recentUsers;
            ViewBag.RecentOffers = recentOffers;
            ViewBag.RecentReviews = recentReviews;


            return View();
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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartDeviceMatch.Data;
using SmartDeviceMatch.Models;

namespace SmartDeviceMatch.Controllers
{
    [Authorize]
    public class OfferController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public OfferController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ==========================================
        // GET: Offer/Create
        // RepairShop creates a repair offer
        // ==========================================

        [Authorize(Roles = "RepairShop")]
        public async Task<IActionResult> Create(int deviceId)
        {
            var repairShop = await GetCurrentRepairShop();

            if (repairShop == null)
            {
                return RedirectToAction(
                    "Create",
                    "Profile");
            }

            var device = await _context.Devices
                .Include(d => d.Category)
                .FirstOrDefaultAsync(d =>
                    d.Id == deviceId &&
                    d.Status == "Listed" &&
                    !d.IsDeleted);

            if (device == null)
            {
                return NotFound();
            }

            ViewBag.Device = device;

            return View();
        }


        // ==========================================
        // POST: Offer/Create
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "RepairShop")]
        public async Task<IActionResult> Create(
            int deviceId,
            decimal offerAmount,
            string? message)
        {
            var repairShop = await GetCurrentRepairShop();

            if (repairShop == null)
            {
                return RedirectToAction(
                    "Create",
                    "Profile");
            }

            var device = await _context.Devices
                .FirstOrDefaultAsync(d =>
                    d.Id == deviceId &&
                    d.Status == "Listed" &&
                    !d.IsDeleted);

            if (device == null)
            {
                return NotFound();
            }

            if (offerAmount <= 0)
            {
                ModelState.AddModelError(
                    "offerAmount",
                    "Offer amount must be greater than zero.");

                ViewBag.Device = device;

                return View();
            }

            var existingOffer = await _context.Offers
                .AnyAsync(o =>
                    o.DeviceId == deviceId &&
                    o.RepairShopId == repairShop.Id &&
                    o.OfferType == "Repair" &&
                    o.Status == "Pending");

            if (existingOffer)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "You already have a pending offer for this device.");

                ViewBag.Device = device;

                return View();
            }

            var offer = new Offer
            {
                DeviceId = deviceId,
                RepairShopId = repairShop.Id,
                BuyerId = null,

                OfferAmount = offerAmount,
                Message = message,

                Currency = "INR",
                OfferType = "Repair",
                Status = "Pending",

                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            _context.Offers.Add(offer);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(MyOffers));
        }


        // ==========================================
        // GET: Offer/MyOffers
        // RepairShop's submitted repair offers
        // ==========================================

        [Authorize(Roles = "RepairShop")]
        public async Task<IActionResult> MyOffers()
        {
            var repairShop = await GetCurrentRepairShop();

            if (repairShop == null)
            {
                return RedirectToAction(
                    "Create",
                    "Profile");
            }

            var offers = await _context.Offers
                .Include(o => o.Device)
                    .ThenInclude(d => d!.Category)
                .Where(o =>
                    o.RepairShopId == repairShop.Id &&
                    o.OfferType == "Repair")
                .OrderByDescending(o => o.Id)
                .ToListAsync();

            return View(offers);
        }


        // ==========================================
        // GET: Offer/Received
        // DeviceOwner receives repair offers
        // ==========================================

        [Authorize(Roles = "DeviceOwner")]
        public async Task<IActionResult> Received()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            var appUser = await _context.AppUsers
                .FirstOrDefaultAsync(u =>
                    u.IdentityUserId == userId);

            if (appUser == null)
            {
                return RedirectToAction(
                    "Create",
                    "Profile");
            }

            var offers = await _context.Offers
                .Include(o => o.Device)
                    .ThenInclude(d => d!.Category)
                .Include(o => o.RepairShop)
                .Where(o =>
                    o.Device != null &&
                    o.Device.OwnerId == appUser.Id &&
                    o.OfferType == "Repair")
                .OrderByDescending(o => o.Id)
                .ToListAsync();

            return View(offers);
        }


        // ==========================================
        // POST: Offer/Accept
        // DeviceOwner accepts repair offer
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "DeviceOwner")]
        public async Task<IActionResult> Accept(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            var appUser = await _context.AppUsers
                .FirstOrDefaultAsync(u =>
                    u.IdentityUserId == userId);

            if (appUser == null)
            {
                return RedirectToAction(
                    "Create",
                    "Profile");
            }

            var offer = await _context.Offers
                .Include(o => o.Device)
                .FirstOrDefaultAsync(o =>
                    o.Id == id &&
                    o.OfferType == "Repair");

            if (offer == null || offer.Device == null)
            {
                return NotFound();
            }

            // Make sure this offer belongs to the
            // logged-in DeviceOwner's device.
            if (offer.Device.OwnerId != appUser.Id)
            {
                return Forbid();
            }

            // Only pending offers can be accepted.
            if (offer.Status != "Pending")
            {
                TempData["OfferMessage"] =
                    "This offer is no longer pending.";

                return RedirectToAction(nameof(Received));
            }

            // Check offer expiry.
            if (offer.ExpiresAt < DateTime.UtcNow)
            {
                offer.Status = "Expired";

                await _context.SaveChangesAsync();

                TempData["OfferMessage"] =
                    "This offer has expired.";

                return RedirectToAction(nameof(Received));
            }

            // Accept selected offer.
            offer.Status = "Accepted";

            // Reject other pending repair offers
            // for the same device.
            var otherOffers = await _context.Offers
                .Where(o =>
                    o.DeviceId == offer.DeviceId &&
                    o.Id != offer.Id &&
                    o.OfferType == "Repair" &&
                    o.Status == "Pending")
                .ToListAsync();

            foreach (var otherOffer in otherOffers)
            {
                otherOffer.Status = "Rejected";
            }

            await _context.SaveChangesAsync();

            TempData["OfferMessage"] =
                "Repair offer accepted successfully.";

            return RedirectToAction(nameof(Received));
        }


        // ==========================================
        // POST: Offer/Reject
        // DeviceOwner rejects repair offer
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "DeviceOwner")]
        public async Task<IActionResult> Reject(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            var appUser = await _context.AppUsers
                .FirstOrDefaultAsync(u =>
                    u.IdentityUserId == userId);

            if (appUser == null)
            {
                return RedirectToAction(
                    "Create",
                    "Profile");
            }

            var offer = await _context.Offers
                .Include(o => o.Device)
                .FirstOrDefaultAsync(o =>
                    o.Id == id &&
                    o.OfferType == "Repair");

            if (offer == null || offer.Device == null)
            {
                return NotFound();
            }

            // Make sure this offer belongs to the
            // logged-in DeviceOwner's device.
            if (offer.Device.OwnerId != appUser.Id)
            {
                return Forbid();
            }

            // Only pending offers can be rejected.
            if (offer.Status != "Pending")
            {
                TempData["OfferMessage"] =
                    "This offer is no longer pending.";

                return RedirectToAction(nameof(Received));
            }

            offer.Status = "Rejected";

            await _context.SaveChangesAsync();

            TempData["OfferMessage"] =
                "Repair offer rejected.";

            return RedirectToAction(nameof(Received));
        }


        // ==========================================
        // Helper:
        // Get current RepairShop
        // ==========================================

        private async Task<RepairShop?> GetCurrentRepairShop()
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

            if (appUser == null)
            {
                return null;
            }

            var repairShop = await _context.RepairShops
                .FirstOrDefaultAsync(r =>
                    r.UserId == appUser.Id);

            if (repairShop != null)
            {
                return repairShop;
            }

            repairShop = new RepairShop
            {
                UserId = appUser.Id,

                ShopName = appUser.FullName,

                Description = null,

                LicenseNumber = null,

                IsVerified = false,

                YearsOfExperience = 0,

                OperatingHours = null,

                City = appUser.City ?? "Not Provided",

                State = appUser.State ?? "Not Provided",

                PinCode = appUser.PinCode ?? "000000",

                Latitude = 0,

                Longitude = 0,

                Rating = 0.0,

                TotalReviews = 0
            };

            _context.RepairShops.Add(repairShop);

            await _context.SaveChangesAsync();

            return repairShop;
        }
    }
}
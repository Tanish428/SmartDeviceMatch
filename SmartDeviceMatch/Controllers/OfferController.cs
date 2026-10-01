using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartDeviceMatch.Data;
using SmartDeviceMatch.Models;
using SmartDeviceMatch.Services;

namespace SmartDeviceMatch.Controllers
{
    [Authorize]
    public class OfferController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        private readonly INotificationService _notificationService;

        public OfferController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            INotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
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


            // Notify DeviceOwner
            await _notificationService.CreateAsync(
                device.OwnerId,
                "New Offer Received",
                $"A repair shop has submitted a repair offer of ₹{offer.OfferAmount:0.00} for your {device.BrandName} {device.ModelName}.",
                "NewOffer",
                offer.Id.ToString(),
                "Offer"
            );

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

            if (offer.Device.OwnerId != appUser.Id)
            {
                return Forbid();
            }

            if (offer.Status != "Pending")
            {
                TempData["OfferMessage"] =
                    "This offer is no longer pending.";

                return RedirectToAction(nameof(Received));
            }

            if (offer.ExpiresAt < DateTime.UtcNow)
            {
                offer.Status = "Expired";

                await _context.SaveChangesAsync();

                TempData["OfferMessage"] =
                    "This offer has expired.";

                return RedirectToAction(nameof(Received));
            }

            // Accept selected offer
            offer.Status = "Accepted";

            // 4.7 - Device enters Repairing status
            offer.Device.Status = "Repairing";
            offer.Device.UpdatedAt = DateTime.UtcNow;

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


            // Notify accepted RepairShop
            if (offer.RepairShopId.HasValue)
            {
                var acceptedRepairShop =
                    await _context.RepairShops
                        .FirstOrDefaultAsync(r =>
                            r.Id == offer.RepairShopId.Value);

                if (acceptedRepairShop != null)
                {
                    await _notificationService.CreateAsync(
                        acceptedRepairShop.UserId,
                        "Offer Accepted",
                        $"Your repair offer for {offer.Device.BrandName} {offer.Device.ModelName} has been accepted.",
                        "OfferAccepted",
                        offer.Id.ToString(),
                        "Offer"
                    );
                }
            }


            // Notify other RepairShops whose offers were rejected
            foreach (var rejectedOffer in otherOffers)
            {
                if (rejectedOffer.RepairShopId.HasValue)
                {
                    var rejectedShop =
                        await _context.RepairShops
                            .FirstOrDefaultAsync(r =>
                                r.Id == rejectedOffer.RepairShopId.Value);

                    if (rejectedShop != null)
                    {
                        await _notificationService.CreateAsync(
                            rejectedShop.UserId,
                            "Offer Rejected",
                            $"Your repair offer for {offer.Device.BrandName} {offer.Device.ModelName} was not selected.",
                            "OfferRejected",
                            rejectedOffer.Id.ToString(),
                            "Offer"
                        );
                    }
                }
            }

            TempData["OfferMessage"] =
                "Repair offer accepted. Device is now being repaired.";

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

            if (offer.Device.OwnerId != appUser.Id)
            {
                return Forbid();
            }

            if (offer.Status != "Pending")
            {
                TempData["OfferMessage"] =
                    "This offer is no longer pending.";

                return RedirectToAction(nameof(Received));
            }

            offer.Status = "Rejected";

            await _context.SaveChangesAsync();


            // Notify RepairShop
            if (offer.RepairShopId.HasValue)
            {
                var repairShop =
                    await _context.RepairShops
                        .FirstOrDefaultAsync(r =>
                            r.Id == offer.RepairShopId.Value);

                if (repairShop != null)
                {
                    await _notificationService.CreateAsync(
                        repairShop.UserId,
                        "Offer Rejected",
                        $"Your repair offer for {offer.Device.BrandName} {offer.Device.ModelName} was rejected.",
                        "OfferRejected",
                        offer.Id.ToString(),
                        "Offer"
                    );
                }
            }

            TempData["OfferMessage"] =
                "Repair offer rejected.";

            return RedirectToAction(nameof(Received));
        }


        // ==========================================
        // GET: /Offer/ReleasePayment
        // RepairShop's ReleasePayment action for accepted purchase transactions
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "RepairShop")]
        public async Task<IActionResult> ReleasePayment(int id)
        {
            var repairShop = await GetCurrentRepairShop();

            if (repairShop == null)
            {
                return RedirectToAction(
                    "Create",
                    "Profile");
            }

            var offer = await _context.Offers
                .Include(o => o.Device)
                .FirstOrDefaultAsync(o =>
                    o.Id == id &&
                    o.RepairShopId == repairShop.Id &&
                    o.OfferType == "Buy");

            if (offer == null || offer.Device == null)
            {
                return NotFound();
            }

            if (offer.Status != "Verified")
            {
                TempData["TransactionMessage"] =
                    "This transaction is not ready for payment release.";

                return RedirectToAction(
                    nameof(MySellerTransactions));
            }

            offer.Status = "Released";
            offer.ReleasedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["TransactionMessage"] =
                "Payment released successfully.";

            return RedirectToAction(
                nameof(MySellerTransactions));
        }


        // ==========================================
        // GET: Offer/MyRepairs
        // RepairShop's active repairs
        // ==========================================

        [Authorize(Roles = "RepairShop")]
        public async Task<IActionResult> MyRepairs()
        {
            var repairShop = await GetCurrentRepairShop();

            if (repairShop == null)
            {
                return RedirectToAction(
                    "Create",
                    "Profile");
            }

            var repairs = await _context.Offers
                .Include(o => o.Device)
                    .ThenInclude(d => d!.Category)
                .Where(o =>
                    o.RepairShopId == repairShop.Id &&
                    o.OfferType == "Repair" &&
                    o.Status == "Accepted" &&
                    o.Device != null &&
                    o.Device.Status == "Repairing")
                .OrderByDescending(o => o.Id)
                .ToListAsync();

            return View(repairs);
        }


        // ==========================================
        // POST: Offer/CompleteRepair
        // RepairShop completes a repair
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "RepairShop")]
        public async Task<IActionResult> CompleteRepair(int id)
        {
            var repairShop = await GetCurrentRepairShop();

            if (repairShop == null)
            {
                return RedirectToAction(
                    "Create",
                    "Profile");
            }

            var offer = await _context.Offers
                .Include(o => o.Device)
                .FirstOrDefaultAsync(o =>
                    o.Id == id &&
                    o.RepairShopId == repairShop.Id &&
                    o.OfferType == "Repair");

            if (offer == null || offer.Device == null)
            {
                return NotFound();
            }

            // Only an accepted repair can be completed.
            if (offer.Status != "Accepted")
            {
                TempData["RepairMessage"] =
                    "This repair is not currently active.";

                return RedirectToAction(nameof(MyRepairs));
            }

            // Make sure the device is actually being repaired.
            if (offer.Device.Status != "Repairing")
            {
                TempData["RepairMessage"] =
                    "This device is not currently in Repairing status.";

                return RedirectToAction(nameof(MyRepairs));
            }

            // ==========================================
            // 4.8 - Complete repair
            // ==========================================

            offer.Status = "Completed";

            offer.Device.Status = "Refurbished";

            offer.Device.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["RepairMessage"] =
                "Repair completed successfully. Device is now refurbished.";

            return RedirectToAction(nameof(MyRepairs));
        }

        // ==========================================
        // GET: Offer/CreatePurchase
        // Buyer creates a purchase offer
        // ==========================================

        [Authorize(Roles = "Buyer")]
        public async Task<IActionResult> CreatePurchase(int deviceId)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            var buyer = await _context.AppUsers
                .FirstOrDefaultAsync(u =>
                    u.IdentityUserId == userId);

            if (buyer == null)
            {
                return RedirectToAction(
                    "Create",
                    "Profile");
            }

            var device = await _context.Devices
                .Include(d => d.Category)
                .FirstOrDefaultAsync(d =>
                    d.Id == deviceId &&
                    d.Status == "Refurbished" &&
                    !d.IsDeleted);

            if (device == null)
            {
                return NotFound();
            }

            // The refurbished device is being sold by
            // the RepairShop that completed the repair.
            var completedRepair = await _context.Offers
                .Include(o => o.RepairShop)
                .FirstOrDefaultAsync(o =>
                    o.DeviceId == deviceId &&
                    o.OfferType == "Repair" &&
                    o.Status == "Completed" &&
                    o.RepairShopId != null);

            if (completedRepair == null ||
                completedRepair.RepairShop == null)
            {
                return NotFound();
            }

            // Buyer cannot make an offer on their own listing
            // if the buyer is also the original owner.
            if (device.OwnerId == buyer.Id)
            {
                TempData["PurchaseMessage"] =
                    "You cannot make a purchase offer for your own device.";

                return RedirectToAction(
                    "Refurbished",
                    "Marketplace");
            }

            ViewBag.Device = device;
            ViewBag.RepairShop = completedRepair.RepairShop;

            return View();
        }

        // ==========================================
        // POST: Offer/CreatePurchase
        // Buyer submits a purchase offer
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Buyer")]
        public async Task<IActionResult> CreatePurchase(
            int deviceId,
            decimal offerAmount,
            string? message)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            var buyer = await _context.AppUsers
                .FirstOrDefaultAsync(u =>
                    u.IdentityUserId == userId);

            if (buyer == null)
            {
                return RedirectToAction(
                    "Create",
                    "Profile");
            }

            var device = await _context.Devices
                .FirstOrDefaultAsync(d =>
                    d.Id == deviceId &&
                    d.Status == "Refurbished" &&
                    !d.IsDeleted);

            if (device == null)
            {
                return NotFound();
            }

            if (device.OwnerId == buyer.Id)
            {
                TempData["PurchaseMessage"] =
                    "You cannot make a purchase offer for your own device.";

                return RedirectToAction(
                    "Refurbished",
                    "Marketplace");
            }

            if (offerAmount <= 0)
            {
                ModelState.AddModelError(
                    "offerAmount",
                    "Purchase offer amount must be greater than zero.");

                var repairForView = await _context.Offers
                    .Include(o => o.RepairShop)
                    .FirstOrDefaultAsync(o =>
                        o.DeviceId == deviceId &&
                        o.OfferType == "Repair" &&
                        o.Status == "Completed" &&
                        o.RepairShopId != null);

                ViewBag.Device = device;

                if (repairForView != null)
                {
                    ViewBag.RepairShop = repairForView.RepairShop;
                }

                return View();
            }

            // Find the RepairShop that completed the repair.
            var completedRepair = await _context.Offers
                .FirstOrDefaultAsync(o =>
                    o.DeviceId == deviceId &&
                    o.OfferType == "Repair" &&
                    o.Status == "Completed" &&
                    o.RepairShopId != null);

            if (completedRepair == null)
            {
                return NotFound();
            }

            // Prevent the same buyer from creating
            // multiple pending purchase offers.
            var existingOffer = await _context.Offers
                .AnyAsync(o =>
                    o.DeviceId == deviceId &&
                    o.BuyerId == buyer.Id &&
                    o.OfferType == "Buy" &&
                    o.Status == "Pending");

            if (existingOffer)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "You already have a pending purchase offer for this device.");

                var repairForView = await _context.Offers
                    .Include(o => o.RepairShop)
                    .FirstOrDefaultAsync(o =>
                        o.DeviceId == deviceId &&
                        o.OfferType == "Repair" &&
                        o.Status == "Completed" &&
                        o.RepairShopId != null);

                ViewBag.Device = device;

                if (repairForView != null)
                {
                    ViewBag.RepairShop = repairForView.RepairShop;
                }

                return View();
            }

            var offer = new Offer
            {
                DeviceId = deviceId,

                // Buyer making the offer
                BuyerId = buyer.Id,

                // RepairShop is the seller of the refurbished device
                RepairShopId = completedRepair.RepairShopId,

                OfferAmount = offerAmount,

                Currency = "INR",

                Message = message,

                OfferType = "Buy",

                Status = "Pending",

                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            _context.Offers.Add(offer);

            await _context.SaveChangesAsync();


            // Notify RepairShop
            if (completedRepair.RepairShopId.HasValue)
            {
                var repairShop =
                    await _context.RepairShops
                        .FirstOrDefaultAsync(r =>
                            r.Id == completedRepair.RepairShopId.Value);

                if (repairShop != null)
                {
                    await _notificationService.CreateAsync(
                        repairShop.UserId,
                        "New Purchase Offer",
                        $"A buyer has submitted a purchase offer of ₹{offer.OfferAmount:0.00} for your refurbished {device.BrandName} {device.ModelName}.",
                        "NewOffer",
                        offer.Id.ToString(),
                        "Offer"
                    );
                }
            }

            TempData["PurchaseMessage"] =
                "Purchase offer submitted successfully.";

            return RedirectToAction(
                nameof(MyPurchaseOffers));
        }

        // ==========================================
        // GET: Offer/MyPurchaseOffers
        // Buyer's submitted purchase offers
        // ==========================================

        [Authorize(Roles = "Buyer")]
        public async Task<IActionResult> MyPurchaseOffers()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            var buyer = await _context.AppUsers
                .FirstOrDefaultAsync(u =>
                    u.IdentityUserId == userId);

            if (buyer == null)
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
                    o.BuyerId == buyer.Id &&
                    o.OfferType == "Buy")
                .OrderByDescending(o => o.Id)
                .ToListAsync();

            return View(offers);
        }

        // ==========================================
        // GET: Offer/PurchaseReceived
        // Purchase offers received by RepairShop
        // ==========================================

        [Authorize(Roles = "RepairShop")]
        public async Task<IActionResult> PurchaseReceived()
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
                .Include(o => o.Buyer)
                .Where(o =>
                    o.RepairShopId == repairShop.Id &&
                    o.OfferType == "Buy")
                .OrderByDescending(o => o.Id)
                .ToListAsync();

            return View(offers);
        }

        // ==========================================
        // POST: Offer/AcceptPurchase
        // RepairShop accepts a buyer purchase offer
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "RepairShop")]
        public async Task<IActionResult> AcceptPurchase(int id)
        {
            var repairShop = await GetCurrentRepairShop();

            if (repairShop == null)
            {
                return RedirectToAction(
                    "Create",
                    "Profile");
            }

            var offer = await _context.Offers
                .Include(o => o.Device)
                .FirstOrDefaultAsync(o =>
                    o.Id == id &&
                    o.RepairShopId == repairShop.Id &&
                    o.OfferType == "Buy");

            if (offer == null || offer.Device == null)
            {
                return NotFound();
            }

            if (offer.Status != "Pending")
            {
                TempData["PurchaseMessage"] =
                    "This purchase offer is no longer pending.";

                return RedirectToAction(
                    nameof(PurchaseReceived));
            }

            if (offer.Device.Status != "Refurbished")
            {
                TempData["PurchaseMessage"] =
                    "This device is not currently available for purchase.";

                return RedirectToAction(
                    nameof(PurchaseReceived));
            }

            // Accept the selected purchase offer
            offer.Status = "Accepted";

            // Reject all other pending purchase offers
            var otherOffers = await _context.Offers
                .Where(o =>
                    o.DeviceId == offer.DeviceId &&
                    o.OfferType == "Buy" &&
                    o.Status == "Pending" &&
                    o.Id != offer.Id)
                .ToListAsync();

            foreach (var otherOffer in otherOffers)
            {
                otherOffer.Status = "Rejected";
            }

            await _context.SaveChangesAsync();


            // Notify buyer whose offer was accepted
            if (offer.BuyerId.HasValue)
            {
                await _notificationService.CreateAsync(
                    offer.BuyerId.Value,
                    "Purchase Offer Accepted",
                    $"Your purchase offer for {offer.Device.BrandName} {offer.Device.ModelName} has been accepted.",
                    "OfferAccepted",
                    offer.Id.ToString(),
                    "Offer"
                );
            }


            // Notify buyers whose offers were rejected
            foreach (var rejectedOffer in otherOffers)
            {
                if (rejectedOffer.BuyerId.HasValue)
                {
                    await _notificationService.CreateAsync(
                        rejectedOffer.BuyerId.Value,
                        "Purchase Offer Rejected",
                        $"Your purchase offer for {offer.Device.BrandName} {offer.Device.ModelName} was not selected.",
                        "OfferRejected",
                        rejectedOffer.Id.ToString(),
                        "Offer"
                    );
                }
            }

            TempData["PurchaseMessage"] =
                "Purchase offer accepted successfully.";

            return RedirectToAction(
                nameof(PurchaseReceived));
        }

        // ==========================================
        // GET: Offer/MySellerTransactions
        // RepairShop views accepted/escrowed purchases
        // ==========================================

        [Authorize(Roles = "RepairShop")]
        public async Task<IActionResult> MySellerTransactions()
        {
            var repairShop = await GetCurrentRepairShop();

            if (repairShop == null)
            {
                return RedirectToAction(
                    "Create",
                    "Profile");
            }

            var transactions = await _context.Offers
                .Include(o => o.Device)
                    .ThenInclude(d => d!.Category)
                .Include(o => o.Buyer)
                .Where(o =>
                    o.RepairShopId == repairShop.Id &&
                    o.OfferType == "Buy" &&
                    (
                        o.Status == "Accepted" ||
                        o.Status == "Escrowed" ||
                        o.Status == "Verified" ||
                        o.Status == "Released" ||
                        o.Status == "Completed"
                    ))
                .OrderByDescending(o => o.Id)
                .ToListAsync();

            return View(transactions);
        }

        // ==========================================
        // GET: Offer/MyTransactions
        // Buyer's accepted purchase transactions
        // ==========================================

        [Authorize(Roles = "Buyer")]
        public async Task<IActionResult> MyBuyerTransactions()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            var buyer = await _context.AppUsers
                .FirstOrDefaultAsync(u =>
                    u.IdentityUserId == userId);

            if (buyer == null)
            {
                return RedirectToAction(
                    "Create",
                    "Profile");
            }

            var transactions = await _context.Offers
                .Include(o => o.Device)
                    .ThenInclude(d => d!.Category)
                .Include(o => o.RepairShop)
                .Where(o =>
                       o.BuyerId == buyer.Id &&
                       o.OfferType == "Buy" &&
                       (o.Status == "Accepted" ||
                        o.Status == "Escrowed" ||
                        o.Status == "Verified" ||
                        o.Status == "Released" ||
                        o.Status == "Completed"))
                .OrderByDescending(o => o.Id)
                .ToListAsync();

            return View(transactions);
        }

        // ==========================================
        // GET: Offer/MyTransactions
        // RepairShop's accepted purchase transactions
        // ==========================================

        [Authorize(Roles = "RepairShop")]
        public async Task<IActionResult>   MyTransactions()
        {
            var repairShop = await GetCurrentRepairShop();

            if (repairShop == null)
            {
                return RedirectToAction(
                    "Create",
                    "Profile");
            }

            var transactions = await _context.Offers
                .Include(o => o.Device)
                    .ThenInclude(d => d!.Category)
                .Include(o => o.Buyer)
                .Where(o =>
                    o.RepairShopId == repairShop.Id &&
                    o.OfferType == "Buy" &&
                    o.Status == "Accepted")
                .OrderByDescending(o => o.Id)
                .ToListAsync();

            return View(transactions);
        }


        // ==========================================
        // POST: Offer/Escrow
        // Buyer places accepted purchase transaction
        // into escrow
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Buyer")]
        public async Task<IActionResult> Escrow(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            var buyer = await _context.AppUsers
                .FirstOrDefaultAsync(u =>
                    u.IdentityUserId == userId);

            if (buyer == null)
            {
                return RedirectToAction(
                    "Create",
                    "Profile");
            }

            var offer = await _context.Offers
                .Include(o => o.Device)
                .FirstOrDefaultAsync(o =>
                    o.Id == id &&
                    o.BuyerId == buyer.Id &&
                    o.OfferType == "Buy");

            if (offer == null || offer.Device == null)
            {
                return NotFound();
            }

            if (offer.Status != "Accepted")
            {
                TempData["TransactionMessage"] =
                    "This transaction is not ready for escrow.";

                return RedirectToAction(
                    nameof(MyBuyerTransactions));
            }

            offer.Status = "Escrowed";
            offer.EscrowedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();


            // Notify RepairShop
            if (offer.RepairShopId.HasValue)
            {
                var repairShop =
                    await _context.RepairShops
                        .FirstOrDefaultAsync(r =>
                            r.Id == offer.RepairShopId.Value);

                if (repairShop != null)
                {
                    await _notificationService.CreateAsync(
                        repairShop.UserId,
                        "Payment Escrowed",
                        $"Payment of ₹{offer.OfferAmount:0.00} has been placed in escrow for {offer.Device.BrandName} {offer.Device.ModelName}.",
                        "EscrowAlert",
                        offer.Id.ToString(),
                        "Offer"
                    );
                }
            }


            // Notify Buyer
            await _notificationService.CreateAsync(
                buyer.Id,
                "Payment Escrowed",
                $"Your payment of ₹{offer.OfferAmount:0.00} has been placed in escrow.",
                "EscrowAlert",
                offer.Id.ToString(),
                "Offer"
            );

            TempData["TransactionMessage"] =
                "Payment has been placed in escrow successfully.";

            return RedirectToAction(
                nameof(MyBuyerTransactions));
        }

        // ==========================================
        // POST: Offer/VerifyDevice
        // Buyer verifies the received device
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Buyer")]
        public async Task<IActionResult> VerifyDevice(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            var buyer = await _context.AppUsers
                .FirstOrDefaultAsync(u =>
                    u.IdentityUserId == userId);

            if (buyer == null)
            {
                return RedirectToAction(
                    "Create",
                    "Profile");
            }

            var offer = await _context.Offers
                .Include(o => o.Device)
                .FirstOrDefaultAsync(o =>
                    o.Id == id &&
                    o.BuyerId == buyer.Id &&
                    o.OfferType == "Buy");

            if (offer == null || offer.Device == null)
            {
                return NotFound();
            }

            if (offer.Status != "Escrowed")
            {
                TempData["TransactionMessage"] =
                    "This transaction is not ready for verification.";

                return RedirectToAction(
                    nameof(MyBuyerTransactions));
            }

            offer.Status = "Verified";
            offer.VerifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();


            // Notify RepairShop
            if (offer.RepairShopId.HasValue)
            {
                var repairShop =
                    await _context.RepairShops
                        .FirstOrDefaultAsync(r =>
                            r.Id == offer.RepairShopId.Value);

                if (repairShop != null)
                {
                    await _notificationService.CreateAsync(
                        repairShop.UserId,
                        "Device Verified",
                        $"The buyer has verified the {offer.Device.BrandName} {offer.Device.ModelName}.",
                        "DeviceVerified",
                        offer.Id.ToString(),
                        "Offer"
                    );
                }
            }


            // Notify Buyer
            await _notificationService.CreateAsync(
                buyer.Id,
                "Device Verified",
                $"You successfully verified the {offer.Device.BrandName} {offer.Device.ModelName}.",
                "DeviceVerified",
                offer.Id.ToString(),
                "Offer"
            );

            TempData["TransactionMessage"] =
                "Device verified successfully.";

            return RedirectToAction(
                nameof(MyBuyerTransactions));
        }

        // ==========================================
        // POST: Offer/CompleteTransaction
        // RepairShop completes the transaction
        // after payment has been released
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "RepairShop")]
        public async Task<IActionResult> CompleteTransaction(int id)
        {
            var repairShop = await GetCurrentRepairShop();

            if (repairShop == null)
            {
                return RedirectToAction(
                    "Create",
                    "Profile");
            }

            var offer = await _context.Offers
                .Include(o => o.Device)
                .FirstOrDefaultAsync(o =>
                    o.Id == id &&
                    o.RepairShopId == repairShop.Id &&
                    o.OfferType == "Buy");

            if (offer == null || offer.Device == null)
            {
                return NotFound();
            }

            if (offer.Status != "Released")
            {
                TempData["TransactionMessage"] =
                    "This transaction is not ready for completion.";

                return RedirectToAction(
                    nameof(MySellerTransactions));
            }

            offer.Status = "Completed";

            await _context.SaveChangesAsync();


            // Notify Buyer
            if (offer.BuyerId.HasValue)
            {
                await _notificationService.CreateAsync(
                    offer.BuyerId.Value,
                    "Transaction Completed",
                    $"Your purchase transaction for {offer.Device.BrandName} {offer.Device.ModelName} has been completed.",
                    "TransactionCompleted",
                    offer.Id.ToString(),
                    "Offer"
                );
            }

            TempData["TransactionMessage"] =
                "Transaction completed successfully.";

            return RedirectToAction(
                nameof(MySellerTransactions));
        }


        // ==========================================
        // GET: Offer/CreateReview
        // Buyer or RepairShop opens review form
        // after transaction is completed
        // ==========================================

        [Authorize(Roles = "Buyer,RepairShop")]
        public async Task<IActionResult> CreateReview(int offerId)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            var reviewer = await _context.AppUsers
                .FirstOrDefaultAsync(u =>
                    u.IdentityUserId == userId);

            if (reviewer == null)
            {
                return RedirectToAction(
                    "Create",
                    "Profile");
            }

            var offer = await _context.Offers
                .Include(o => o.Device)
                    .ThenInclude(d => d!.Category)
                .Include(o => o.RepairShop)
                .Include(o => o.Buyer)
                .FirstOrDefaultAsync(o =>
                    o.Id == offerId &&
                    o.OfferType == "Buy" &&
                    o.Status == "Completed");

            if (offer == null || offer.Device == null)
            {
                return NotFound();
            }

            int revieweeId;

            // Buyer is reviewing RepairShop
            if (offer.BuyerId == reviewer.Id)
            {
                if (offer.RepairShop == null)
                {
                    return NotFound();
                }

                revieweeId = offer.RepairShop.UserId;
            }
            // RepairShop is reviewing Buyer
            else if (offer.RepairShop != null &&
                     offer.RepairShop.UserId == reviewer.Id)
            {
                if (!offer.BuyerId.HasValue)
                {
                    return NotFound();
                }

                revieweeId = offer.BuyerId.Value;
            }
            else
            {
                return Forbid();
            }

            // Prevent duplicate review
            var existingReview = await _context.Reviews
                .AnyAsync(r =>
                    r.OfferId == offerId &&
                    r.ReviewerId == reviewer.Id);

            if (existingReview)
            {
                TempData["ReviewMessage"] =
                    "You have already reviewed this transaction.";

                if (reviewer.Id == offer.BuyerId)
                {
                    return RedirectToAction(
                        nameof(MyBuyerTransactions));
                }

                return RedirectToAction(
                    nameof(MySellerTransactions));
            }

            ViewBag.Offer = offer;
            ViewBag.RevieweeId = revieweeId;

            return View();
        }

        // ==========================================
        // POST: Offer/CreateReview
        // Saves Buyer/RepairShop review
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Buyer,RepairShop")]
        public async Task<IActionResult> CreateReview(
            int offerId,
            int rating,
            string? comment,
            bool isAnonymous)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            var reviewer = await _context.AppUsers
                .FirstOrDefaultAsync(u =>
                    u.IdentityUserId == userId);

            if (reviewer == null)
            {
                return RedirectToAction(
                    "Create",
                    "Profile");
            }

            var offer = await _context.Offers
                .Include(o => o.Device)
                    .ThenInclude(d => d!.Category)
                .Include(o => o.RepairShop)
                .Include(o => o.Buyer)
                .FirstOrDefaultAsync(o =>
                    o.Id == offerId &&
                    o.OfferType == "Buy" &&
                    o.Status == "Completed");

            if (offer == null || offer.Device == null)
            {
                return NotFound();
            }

            int revieweeId;

            // Buyer reviews RepairShop
            if (offer.BuyerId == reviewer.Id)
            {
                if (offer.RepairShop == null)
                {
                    return NotFound();
                }

                revieweeId = offer.RepairShop.UserId;
            }
            // RepairShop reviews Buyer
            else if (offer.RepairShop != null &&
                     offer.RepairShop.UserId == reviewer.Id)
            {
                if (!offer.BuyerId.HasValue)
                {
                    return NotFound();
                }

                revieweeId = offer.BuyerId.Value;
            }
            else
            {
                return Forbid();
            }

            // Validate rating
            if (rating < 1 || rating > 5)
            {
                ModelState.AddModelError(
                    "rating",
                    "Rating must be between 1 and 5.");
            }

            // Prevent duplicate review
            var existingReview = await _context.Reviews
                .AnyAsync(r =>
                    r.OfferId == offerId &&
                    r.ReviewerId == reviewer.Id);

            if (existingReview)
            {
                TempData["ReviewMessage"] =
                    "You have already reviewed this transaction.";

                if (reviewer.Id == offer.BuyerId)
                {
                    return RedirectToAction(
                        nameof(MyBuyerTransactions));
                }

                return RedirectToAction(
                    nameof(MySellerTransactions));
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Offer = offer;
                ViewBag.RevieweeId = revieweeId;

                return View();
            }

            var review = new Review
            {
                OfferId = offer.Id,
                ReviewerId = reviewer.Id,
                RevieweeId = revieweeId,
                Rating = rating,
                Comment = comment,
                IsAnonymous = isAnonymous,
                CreatedAt = DateTime.UtcNow
            };

            _context.Reviews.Add(review);

            await _context.SaveChangesAsync();

            TempData["ReviewMessage"] =
                "Review submitted successfully.";

            if (reviewer.Id == offer.BuyerId)
            {
                return RedirectToAction(
                    nameof(MyBuyerTransactions));
            }

            return RedirectToAction(
                nameof(MySellerTransactions));
        }

        // ==========================================
        // GET: Offer/MyReviews
        // Shows reviews received by current user
        // and calculates average rating
        // ==========================================

        [Authorize(Roles = "Buyer,RepairShop")]
        public async Task<IActionResult> MyReviews()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            var currentUser = await _context.AppUsers
                .FirstOrDefaultAsync(u =>
                    u.IdentityUserId == userId);

            if (currentUser == null)
            {
                return RedirectToAction(
                    "Create",
                    "Profile");
            }

            var reviews = await _context.Reviews
                .Include(r => r.Offer)
                    .ThenInclude(o => o!.Device)
                        .ThenInclude(d => d!.Category)
                .Include(r => r.Reviewer)
                .Where(r => r.RevieweeId == currentUser.Id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();


            // ==========================================
            // Calculate average rating
            // ==========================================

            double averageRating = reviews.Any()
                ? reviews.Average(r => r.Rating)
                : 0;


            // ==========================================
            // Send rating information to View
            // ==========================================

            ViewBag.AverageRating = averageRating;
            ViewBag.ReviewCount = reviews.Count;


            return View(reviews);
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

        // ==========================================
        // GET: Offer/ReportDispute
        // ==========================================

        public async Task<IActionResult> ReportDispute(int offerId)
        {
            var identityUserId = _userManager.GetUserId(User);

            if (identityUserId == null)
            {
                return Challenge();
            }

            var appUser = await _context.AppUsers
                .FirstOrDefaultAsync(u =>
                    u.IdentityUserId == identityUserId);

            if (appUser == null)
            {
                return RedirectToAction("Create", "Profile");
            }

            var offer = await _context.Offers
                .Include(o => o.Device)
                .Include(o => o.RepairShop)
                .FirstOrDefaultAsync(o => o.Id == offerId);

            if (offer == null || offer.Device == null)
            {
                return NotFound();
            }

            bool isParticipant =
                offer.Device.OwnerId == appUser.Id ||
                offer.BuyerId == appUser.Id ||
                offer.RepairShop?.UserId == appUser.Id;

            if (!isParticipant)
            {
                return Forbid();
            }

            ViewBag.OfferId = offer.Id;
            ViewBag.DeviceName =
                $"{offer.Device.BrandName} {offer.Device.ModelName}";

            return View();
        }

        // ==========================================
        // POST: Offer/ReportDispute
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportDispute(
            int offerId,
            string reason)
        {
            var identityUserId = _userManager.GetUserId(User);

            if (identityUserId == null)
            {
                return Challenge();
            }

            var appUser = await _context.AppUsers
                .FirstOrDefaultAsync(u =>
                    u.IdentityUserId == identityUserId);

            if (appUser == null)
            {
                return RedirectToAction("Create", "Profile");
            }

            var offer = await _context.Offers
                .Include(o => o.Device)
                .Include(o => o.RepairShop)
                .FirstOrDefaultAsync(o => o.Id == offerId);

            if (offer == null || offer.Device == null)
            {
                return NotFound();
            }

            bool isParticipant =
                offer.Device.OwnerId == appUser.Id ||
                offer.BuyerId == appUser.Id ||
                offer.RepairShop?.UserId == appUser.Id;

            if (!isParticipant)
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(reason) ||
                reason.Trim().Length > 1000)
            {
                ModelState.AddModelError(
                    "reason",
                    "Enter a reason between 1 and 1000 characters.");

                ViewBag.OfferId = offer.Id;
                ViewBag.DeviceName =
                    $"{offer.Device.BrandName} {offer.Device.ModelName}";

                return View();
            }

            // Prevent multiple open disputes for the same offer
            bool existingDispute = await _context.Disputes
                .AnyAsync(d =>
                    d.OfferId == offerId &&
                    (d.Status == "Open" ||
                     d.Status == "UnderReview"));

            if (existingDispute)
            {
                TempData["DisputeMessage"] =
                    "An open dispute already exists for this offer.";

                return RedirectToAction(
                    "MyOffers",
                    "Offer");
            }

            var dispute = new Dispute
            {
                OfferId = offerId,
                RaisedByUserId = appUser.Id,
                Reason = reason.Trim(),
                Status = "Open",
                CreatedAt = DateTime.UtcNow
            };

            _context.Disputes.Add(dispute);

            await _context.SaveChangesAsync();

            TempData["DisputeMessage"] =
                "Your dispute has been submitted successfully.";

            return RedirectToAction(
                "MyOffers",
                "Offer");
        }
    }
}
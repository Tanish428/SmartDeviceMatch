using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartDeviceMatch.Data;
using SmartDeviceMatch.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SmartDeviceMatch.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public ProfileController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }


        // ==========================================
        // GET: Profile/Index
        // Shows current user's profile and rating
        // ==========================================

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }


            var profile = await _context.AppUsers
                .FirstOrDefaultAsync(u =>
                    u.IdentityUserId == userId);


            // Profile does not exist yet
            if (profile == null)
            {
                return RedirectToAction(
                    nameof(Create));
            }


            // ==========================================
            // Get reviews received by this user
            // ==========================================

            var reviews = await _context.Reviews
                .Where(r =>
                    r.RevieweeId == profile.Id)
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


            return View(profile);
        }


        // ==========================================
        // GET: Profile/Create
        // Creates the user's profile
        // ==========================================

        public async Task<IActionResult> Create()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }


            var existingProfile = await _context.AppUsers
                .FirstOrDefaultAsync(u =>
                    u.IdentityUserId == userId);


            if (existingProfile != null)
            {
                // Make sure an existing RepairShop user
                // also has a RepairShop profile.
                if (existingProfile.UserType == "RepairShop")
                {
                    var repairShopExists =
                        await _context.RepairShops
                            .AnyAsync(r =>
                                r.UserId == existingProfile.Id);


                    if (!repairShopExists)
                    {
                        var repairShop = new RepairShop
                        {
                            UserId = existingProfile.Id,
                            ShopName = existingProfile.FullName,
                            City = existingProfile.City ?? "Not Provided",
                            State = existingProfile.State ?? "Not Provided",
                            PinCode = existingProfile.PinCode ?? "000000",
                            YearsOfExperience = 0,
                            IsVerified = false,
                            Rating = 0.0,
                            TotalReviews = 0
                        };

                        _context.RepairShops.Add(repairShop);

                        await _context.SaveChangesAsync();
                    }
                }


                return RedirectToAction(
                    "Index",
                    "Home");
            }


            return View();
        }


        // ==========================================
        // POST: Profile/Create
        // Saves the user's profile
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("FullName,UserType,City,State,PinCode")]
            AppUser appUser)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }


            var identityUser =
                await _userManager.FindByIdAsync(userId);


            if (identityUser == null)
            {
                return Challenge();
            }


            var existingProfile =
                await _context.AppUsers
                    .AnyAsync(u =>
                        u.IdentityUserId == userId);


            if (existingProfile)
            {
                return RedirectToAction(
                    "Index",
                    "Home");
            }


            appUser.IdentityUserId = userId;

            appUser.CreatedAt = DateTime.UtcNow;
            appUser.IsVerified = false;
            appUser.IsBanned = false;
            appUser.Rating = 0.0;


            ModelState.Remove("IdentityUserId");


            if (!ModelState.IsValid)
            {
                return View(appUser);
            }


            // ==========================================
            // Create AppUser
            // ==========================================

            _context.AppUsers.Add(appUser);

            await _context.SaveChangesAsync();


            // ==========================================
            // Assign Identity role
            // ==========================================

            var roleResult =
                await _userManager.AddToRoleAsync(
                    identityUser,
                    appUser.UserType);


            if (!roleResult.Succeeded)
            {
                foreach (var error in roleResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                return View(appUser);
            }


            // ==========================================
            // Create RepairShop profile automatically
            // ==========================================

            if (appUser.UserType == "RepairShop")
            {
                var repairShop = new RepairShop
                {
                    UserId = appUser.Id,
                    ShopName = appUser.FullName,
                    City = appUser.City ?? "Not Provided",
                    State = appUser.State ?? "Not Provided",
                    PinCode = appUser.PinCode ?? "000000",
                    YearsOfExperience = 0,
                    IsVerified = false,
                    Rating = 0.0,
                    TotalReviews = 0
                };

                _context.RepairShops.Add(repairShop);

                await _context.SaveChangesAsync();
            }


            // ==========================================
            // End temporary login
            // ==========================================

            await _signInManager.SignOutAsync();


            // ==========================================
            // Send user to Login
            // ==========================================

            return RedirectToPage(
                "/Account/Login",
                new
                {
                    area = "Identity"
                });
        }
    }
}
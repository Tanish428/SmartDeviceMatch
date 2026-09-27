using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartDeviceMatch.Data;
using SmartDeviceMatch.Models;
using System;
using System.Threading.Tasks;

namespace SmartDeviceMatch.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public ProfileController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // GET: Profile/Create
        public async Task<IActionResult> Create()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
                return Challenge();

            // Check if profile already exists
            var existingProfile = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.IdentityUserId == userId);

            if (existingProfile != null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // POST: Profile/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("FullName,UserType,City,State,PinCode")]
            AppUser appUser)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
                return Challenge();

            // Get the Identity user
            var identityUser = await _userManager.FindByIdAsync(userId);

            if (identityUser == null)
                return Challenge();

            // Prevent duplicate AppUser
            var existingProfile = await _context.AppUsers
                .AnyAsync(u => u.IdentityUserId == userId);

            if (existingProfile)
            {
                return RedirectToAction("Index", "Home");
            }

            // Link AppUser with IdentityUser
            appUser.IdentityUserId = userId;

            // Set default values
            appUser.CreatedAt = DateTime.UtcNow;
            appUser.IsVerified = false;
            appUser.IsBanned = false;
            appUser.Rating = 0.0;

            // IdentityUserId is assigned manually
            ModelState.Remove("IdentityUserId");

            if (ModelState.IsValid)
            {
                // Save AppUser profile
                _context.AppUsers.Add(appUser);
                await _context.SaveChangesAsync();

                // Assign selected Identity role
                var roleResult = await _userManager.AddToRoleAsync(
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

                // End the temporary login
                await _signInManager.SignOutAsync();

                // Send user to Login
                return RedirectToPage(
                    "/Account/Login",
                    new { area = "Identity" });
            }

            return View(appUser);
        }
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartDeviceMatch.Data;
using SmartDeviceMatch.Models;

namespace SmartDeviceMatch.Controllers
{
    // The [Authorize] tag ensures only users who are logged in can access this page
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ProfileController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 1. GET: Shows the form to the user
        public IActionResult Create()
        {
            // Grab the currently logged-in user's hidden Microsoft ID
            var userId = _userManager.GetUserId(User);

            // Safety check: If they already have a profile in our database, send them to the Home page
            var existingProfile = _context.AppUsers.FirstOrDefault(u => u.IdentityUserId == userId);
            if (existingProfile != null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // 2. POST: Saves the form data into the AppUser table
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create([Bind("FullName,UserType,City,State,PinCode")] AppUser appUser)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge(); // Forces them to log in if their session expired

            // Behind the scenes, link this new profile to their Microsoft Login ID
            appUser.IdentityUserId = userId;

            // Set default values for the background properties
            appUser.CreatedAt = DateTime.UtcNow;
            appUser.IsVerified = false;
            appUser.IsBanned = false;
            appUser.Rating = 0.0;

            // Remove ModelState errors for properties we are assigning manually behind the scenes
            ModelState.Remove("IdentityUserId");

            if (ModelState.IsValid)
            {
                _context.AppUsers.Add(appUser);
                _context.SaveChanges();

                // Once saved, send them to the homepage to start using the app!
                return RedirectToAction("Index", "Home");
            }

            return View(appUser);
        }
    }
}
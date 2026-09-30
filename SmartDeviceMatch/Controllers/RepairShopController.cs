using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartDeviceMatch.Data;
using SmartDeviceMatch.Models;

namespace SmartDeviceMatch.Controllers
{
    [Authorize(Roles = "RepairShop")]
    public class RepairShopController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public RepairShopController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        // =========================================================
        // GET: RepairShop/Index
        // =========================================================

        public async Task<IActionResult> Index()
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

            var repairShop = await _context.RepairShops
                .Include(r => r.User)
                .FirstOrDefaultAsync(r =>
                    r.UserId == appUser.Id);

            if (repairShop == null)
            {
                TempData["ShopMessage"] =
                    "Repair shop profile was not found.";

                return RedirectToAction(
                    "Index",
                    "Home");
            }

            var specializations = await _context.ShopSpecializations
                .Include(s => s.Category)
                .Where(s => s.ShopId == repairShop.Id)
                .OrderBy(s => s.Category!.Name)
                .ThenBy(s => s.BrandName)
                .ToListAsync();

            ViewBag.Specializations = specializations;

            return View(repairShop);
        }


        // =========================================================
        // GET: RepairShop/Edit
        // =========================================================

        public async Task<IActionResult> Edit()
        {
            var repairShop = await GetCurrentRepairShopAsync();

            if (repairShop == null)
            {
                return NotFound();
            }

            return View(repairShop);
        }


        // =========================================================
        // POST: RepairShop/Edit
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind(
                "Id,ShopName,Description,LicenseNumber," +
                "YearsOfExperience,OperatingHours," +
                "City,State,PinCode,Latitude,Longitude")]
            RepairShop model)
        {
            var currentShop = await GetCurrentRepairShopAsync();

            if (currentShop == null)
            {
                return NotFound();
            }

            // Prevent one repair shop user from editing
            // another shop by changing the ID manually.
            if (currentShop.Id != id)
            {
                return Forbid();
            }

            // Verification is controlled by Admin.
            // Do not bind or modify IsVerified here.

            if (model.YearsOfExperience < 0)
            {
                ModelState.AddModelError(
                    "YearsOfExperience",
                    "Years of experience cannot be negative.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            currentShop.ShopName = model.ShopName;
            currentShop.Description = model.Description;
            currentShop.LicenseNumber = model.LicenseNumber;
            currentShop.YearsOfExperience =
                model.YearsOfExperience;
            currentShop.OperatingHours =
                model.OperatingHours;
            currentShop.City = model.City;
            currentShop.State = model.State;
            currentShop.PinCode = model.PinCode;
            currentShop.Latitude = model.Latitude;
            currentShop.Longitude = model.Longitude;

            await _context.SaveChangesAsync();

            TempData["ShopMessage"] =
                "Shop profile updated successfully.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // GET: RepairShop/Specializations
        // =========================================================

        public async Task<IActionResult> Specializations()
        {
            var repairShop = await GetCurrentRepairShopAsync();

            if (repairShop == null)
            {
                return NotFound();
            }

            var specializations =
                await _context.ShopSpecializations
                    .Include(s => s.Category)
                    .Where(s => s.ShopId == repairShop.Id)
                    .OrderBy(s => s.Category!.Name)
                    .ThenBy(s => s.BrandName)
                    .ToListAsync();

            return View(specializations);
        }


        // =========================================================
        // GET: RepairShop/AddSpecialization
        // =========================================================

        public async Task<IActionResult> AddSpecialization()
        {
            var repairShop = await GetCurrentRepairShopAsync();

            if (repairShop == null)
            {
                return NotFound();
            }

            await LoadCategoriesAsync();

            return View();
        }


        // =========================================================
        // POST: RepairShop/AddSpecialization
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSpecialization(
            [Bind(
                "CategoryId,BrandName,ExpertiseLevel,Description")]
            ShopSpecialization model)
        {
            var repairShop = await GetCurrentRepairShopAsync();

            if (repairShop == null)
            {
                return NotFound();
            }

            if (model.ExpertiseLevel < 1 ||
                model.ExpertiseLevel > 5)
            {
                ModelState.AddModelError(
                    "ExpertiseLevel",
                    "Expertise level must be between 1 and 5.");
            }

            var categoryExists =
                await _context.DeviceCategories
                    .AnyAsync(c => c.Id == model.CategoryId);

            if (!categoryExists)
            {
                ModelState.AddModelError(
                    "CategoryId",
                    "Please select a valid category.");
            }

            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync(model.CategoryId);
                return View(model);
            }

            // Prevent the same shop from accidentally
            // adding exactly the same category + brand.
            var duplicateExists =
                await _context.ShopSpecializations
                    .AnyAsync(s =>
                        s.ShopId == repairShop.Id &&
                        s.CategoryId == model.CategoryId &&
                        s.BrandName == model.BrandName);

            if (duplicateExists)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "This specialization already exists.");

                await LoadCategoriesAsync(model.CategoryId);
                return View(model);
            }

            model.ShopId = repairShop.Id;

            _context.ShopSpecializations.Add(model);

            await _context.SaveChangesAsync();

            TempData["ShopMessage"] =
                "Specialization added successfully.";

            return RedirectToAction(
                nameof(Specializations));
        }


        // =========================================================
        // GET: RepairShop/EditSpecialization
        // =========================================================

        public async Task<IActionResult> EditSpecialization(int id)
        {
            var repairShop = await GetCurrentRepairShopAsync();

            if (repairShop == null)
            {
                return NotFound();
            }

            var specialization =
                await _context.ShopSpecializations
                    .FirstOrDefaultAsync(s =>
                        s.Id == id &&
                        s.ShopId == repairShop.Id);

            if (specialization == null)
            {
                return NotFound();
            }

            await LoadCategoriesAsync(
                specialization.CategoryId);

            return View(specialization);
        }


        // =========================================================
        // POST: RepairShop/EditSpecialization
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSpecialization(
            int id,
            [Bind(
                "Id,CategoryId,BrandName," +
                "ExpertiseLevel,Description")]
            ShopSpecialization model)
        {
            var repairShop = await GetCurrentRepairShopAsync();

            if (repairShop == null)
            {
                return NotFound();
            }

            var specialization =
                await _context.ShopSpecializations
                    .FirstOrDefaultAsync(s =>
                        s.Id == id &&
                        s.ShopId == repairShop.Id);

            if (specialization == null)
            {
                return NotFound();
            }

            if (model.ExpertiseLevel < 1 ||
                model.ExpertiseLevel > 5)
            {
                ModelState.AddModelError(
                    "ExpertiseLevel",
                    "Expertise level must be between 1 and 5.");
            }

            var categoryExists =
                await _context.DeviceCategories
                    .AnyAsync(c => c.Id == model.CategoryId);

            if (!categoryExists)
            {
                ModelState.AddModelError(
                    "CategoryId",
                    "Please select a valid category.");
            }

            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync(model.CategoryId);
                return View(model);
            }

            var duplicateExists =
                await _context.ShopSpecializations
                    .AnyAsync(s =>
                        s.Id != id &&
                        s.ShopId == repairShop.Id &&
                        s.CategoryId == model.CategoryId &&
                        s.BrandName == model.BrandName);

            if (duplicateExists)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "This specialization already exists.");

                await LoadCategoriesAsync(model.CategoryId);
                return View(model);
            }

            specialization.CategoryId =
                model.CategoryId;

            specialization.BrandName =
                model.BrandName;

            specialization.ExpertiseLevel =
                model.ExpertiseLevel;

            specialization.Description =
                model.Description;

            await _context.SaveChangesAsync();

            TempData["ShopMessage"] =
                "Specialization updated successfully.";

            return RedirectToAction(
                nameof(Specializations));
        }


        // =========================================================
        // POST: RepairShop/DeleteSpecialization
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSpecialization(
            int id)
        {
            var repairShop = await GetCurrentRepairShopAsync();

            if (repairShop == null)
            {
                return NotFound();
            }

            var specialization =
                await _context.ShopSpecializations
                    .FirstOrDefaultAsync(s =>
                        s.Id == id &&
                        s.ShopId == repairShop.Id);

            if (specialization == null)
            {
                return NotFound();
            }

            _context.ShopSpecializations.Remove(
                specialization);

            await _context.SaveChangesAsync();

            TempData["ShopMessage"] =
                "Specialization removed successfully.";

            return RedirectToAction(
                nameof(Specializations));
        }


        // =========================================================
        // Helper: Get current RepairShop
        // =========================================================

        private async Task<RepairShop?> GetCurrentRepairShopAsync()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return null;
            }

            var appUser = await _context.AppUsers
                .FirstOrDefaultAsync(u =>
                    u.IdentityUserId == userId);

            if (appUser == null)
            {
                return null;
            }

            return await _context.RepairShops
                .FirstOrDefaultAsync(r =>
                    r.UserId == appUser.Id);
        }


        // =========================================================
        // Helper: Load device categories
        // =========================================================

        private async Task LoadCategoriesAsync(
            int? selectedCategoryId = null)
        {
            var categories =
                await _context.DeviceCategories
                    .OrderBy(c => c.Name)
                    .ToListAsync();

            ViewBag.CategoryId =
                new SelectList(
                    categories,
                    "Id",
                    "Name",
                    selectedCategoryId);
        }
    }
}
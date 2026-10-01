
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartDeviceMatch.Data;
using SmartDeviceMatch.Models;

namespace SmartDeviceMatch.Controllers
{
    [Authorize(Roles = "RepairShop,Buyer")]
    public class MarketplaceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MarketplaceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Marketplace/BrokenDevices
        [Authorize(Roles = "RepairShop")]
        public async Task<IActionResult> BrokenDevices(
            string? searchTerm,
            int? categoryId,
            string? brand,
            string? city,
            string? conditionGrade,
            string? damageType)
        {
            // Base query: only active, non-deleted devices
            var query = _context.Devices
                .AsNoTracking()
                .Include(d => d.Category)
                .Include(d => d.Images)
                .Where(d =>
                    d.Status == "Listed" &&
                    !d.IsDeleted);

            // General search across device information
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.Trim();

                query = query.Where(d =>
                    d.BrandName.Contains(searchTerm) ||
                    d.ModelName.Contains(searchTerm) ||
                    d.DamageType.Contains(searchTerm) ||
                    d.ConditionDescription.Contains(searchTerm) ||
                    d.Category!.Name.Contains(searchTerm) ||
                    d.City.Contains(searchTerm) ||
                    d.State.Contains(searchTerm) ||
                    d.PinCode.Contains(searchTerm));
            }

            // Category filter
            if (categoryId.HasValue)
            {
                query = query.Where(d =>
                    d.CategoryId == categoryId.Value);
            }

            // Brand filter
            if (!string.IsNullOrWhiteSpace(brand))
            {
                brand = brand.Trim();

                query = query.Where(d =>
                    d.BrandName == brand);
            }

            // City, State, or PIN code filter
            if (!string.IsNullOrWhiteSpace(city))
            {
                city = city.Trim();

                query = query.Where(d =>
                    d.City.Contains(city) ||
                    d.State.Contains(city) ||
                    d.PinCode.Contains(city));
            }

            // Condition filter
            if (!string.IsNullOrWhiteSpace(conditionGrade))
            {
                query = query.Where(d =>
                    d.ConditionGrade == conditionGrade);
            }

            // Damage type filter
            if (!string.IsNullOrWhiteSpace(damageType))
            {
                query = query.Where(d =>
                    d.DamageType == damageType);
            }

            // Execute filtered query
            var devices = await query
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            // Category dropdown
            ViewBag.Categories = new SelectList(
                await _context.DeviceCategories
                    .AsNoTracking()
                    .OrderBy(c => c.Name)
                    .ToListAsync(),
                "Id",
                "Name",
                categoryId);

            // Available brands
            ViewBag.Brands = new SelectList(
                await _context.Devices
                    .AsNoTracking()
                    .Where(d =>
                        d.Status == "Listed" &&
                        !d.IsDeleted)
                    .Select(d => d.BrandName)
                    .Distinct()
                    .OrderBy(b => b)
                    .ToListAsync(),
                brand);

            // Available condition grades
            ViewBag.ConditionGrades = await _context.Devices
                .AsNoTracking()
                .Where(d =>
                    d.Status == "Listed" &&
                    !d.IsDeleted)
                .Select(d => d.ConditionGrade)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            // Available damage types
            ViewBag.DamageTypes = await _context.Devices
                .AsNoTracking()
                .Where(d =>
                    d.Status == "Listed" &&
                    !d.IsDeleted)
                .Select(d => d.DamageType)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            // Preserve selected filter values
            ViewBag.SearchTerm = searchTerm;
            ViewBag.SelectedBrand = brand;
            ViewBag.SelectedCity = city;
            ViewBag.SelectedConditionGrade = conditionGrade;
            ViewBag.SelectedDamageType = damageType;

            return View(devices);
        }

        // GET: Marketplace/Details/5
        [Authorize(Roles = "RepairShop,Buyer")]
        public async Task<IActionResult> Details(int id)
        {
            var device = await _context.Devices
                .Include(d => d.Category)
                .Include(d => d.Images)
                .FirstOrDefaultAsync(d =>
                    d.Id == id &&
                    d.Status == "Listed" &&
                    !d.IsDeleted);

            if (device == null)
            {
                return NotFound();
            }

            device.ViewCount++;

            await _context.SaveChangesAsync();

            return View(device);
        }

        // GET: Marketplace/Refurbished
        [Authorize(Roles = "Buyer")]
        public async Task<IActionResult> Refurbished(
            int? categoryId,
            string? conditionGrade,
            string? damageType)
        {
            // Existing refurbished workflow
            var devices = await _context.Devices
                .Include(d => d.Category)
                .Include(d => d.Images)
                .Where(d =>
                    d.Status == "Refurbished" &&
                    !d.IsDeleted)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            return View(devices);
        }
    }
}
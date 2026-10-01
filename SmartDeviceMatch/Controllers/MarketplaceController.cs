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
            var query = _context.Devices
                .AsNoTracking()
                .Include(d => d.Category)
                .Include(d => d.Images)
                .Where(d =>
                    d.Status == "Listed" &&
                    !d.IsDeleted);

            // General search
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

            // Damage filter
            if (!string.IsNullOrWhiteSpace(damageType))
            {
                query = query.Where(d =>
                    d.DamageType == damageType);
            }

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

            // Brand dropdown
            var brands = await _context.Devices
                .AsNoTracking()
                .Where(d =>
                    d.Status == "Listed" &&
                    !d.IsDeleted)
                .Select(d => d.BrandName)
                .Distinct()
                .OrderBy(b => b)
                .ToListAsync();

            ViewBag.Brands = new SelectList(brands, brand);

            // Condition options
            ViewBag.ConditionGrades = await _context.Devices
                .AsNoTracking()
                .Where(d =>
                    d.Status == "Listed" &&
                    !d.IsDeleted)
                .Select(d => d.ConditionGrade)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            // Damage options
            ViewBag.DamageTypes = await _context.Devices
                .AsNoTracking()
                .Where(d =>
                    d.Status == "Listed" &&
                    !d.IsDeleted)
                .Select(d => d.DamageType)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            // Preserve filter values
            ViewBag.SearchTerm = searchTerm;
            ViewBag.SelectedBrand = brand;
            ViewBag.SelectedCity = city;
            ViewBag.SelectedConditionGrade = conditionGrade;
            ViewBag.SelectedDamageType = damageType;

            return View(devices);
        }

        // GET: Marketplace/Details/12
        [HttpGet]
        [Authorize(Roles = "RepairShop,Buyer")]
        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            var device = await _context.Devices
                .AsNoTracking()
                .Include(d => d.Category)
                .Include(d => d.Images)
                .FirstOrDefaultAsync(d =>
                    d.Id == id &&
                    !d.IsDeleted &&
                    (d.Status == "Listed" ||
                     d.Status == "Refurbished"));

            if (device == null)
            {
                return NotFound();
            }

            // Increment view count separately
            await _context.Devices
                .Where(d => d.Id == id)
                .ExecuteUpdateAsync(setters =>
                    setters.SetProperty(
                        d => d.ViewCount,
                        d => d.ViewCount + 1));

            return View(device);
        }

        // GET: Marketplace/Refurbished
        [HttpGet]
        [Authorize(Roles = "Buyer")]
        public async Task<IActionResult> Refurbished(
            int? categoryId,
            string? conditionGrade,
            string? damageType)
        {
            var query = _context.Devices
                .AsNoTracking()
                .Include(d => d.Category)
                .Include(d => d.Images)
                .Where(d =>
                    d.Status == "Refurbished" &&
                    !d.IsDeleted);

            // Category filter
            if (categoryId.HasValue)
            {
                query = query.Where(d =>
                    d.CategoryId == categoryId.Value);
            }

            // Condition filter
            if (!string.IsNullOrWhiteSpace(conditionGrade))
            {
                query = query.Where(d =>
                    d.ConditionGrade == conditionGrade);
            }

            // Damage filter
            if (!string.IsNullOrWhiteSpace(damageType))
            {
                query = query.Where(d =>
                    d.DamageType == damageType);
            }

            var devices = await query
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            ViewBag.Categories = new SelectList(
                await _context.DeviceCategories
                    .AsNoTracking()
                    .OrderBy(c => c.Name)
                    .ToListAsync(),
                "Id",
                "Name",
                categoryId);

            ViewBag.ConditionGrades = await _context.Devices
                .AsNoTracking()
                .Where(d =>
                    d.Status == "Refurbished" &&
                    !d.IsDeleted)
                .Select(d => d.ConditionGrade)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            ViewBag.DamageTypes = await _context.Devices
                .AsNoTracking()
                .Where(d =>
                    d.Status == "Refurbished" &&
                    !d.IsDeleted)
                .Select(d => d.DamageType)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            ViewBag.SelectedCategoryId = categoryId;
            ViewBag.SelectedConditionGrade = conditionGrade;
            ViewBag.SelectedDamageType = damageType;

            return View(devices);
        }
    }
}


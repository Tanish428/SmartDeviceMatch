using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartDeviceMatch.Data;

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
            int? categoryId,
            string? conditionGrade,
            string? damageType)
        {
            var query = _context.Devices
                .Include(d => d.Category)
                .Include(d => d.Images)
                .Where(d =>
                    d.Status == "Listed" &&
                    !d.IsDeleted);

            // Filter by category
            if (categoryId.HasValue)
            {
                query = query.Where(d =>
                    d.CategoryId == categoryId.Value);
            }

            // Filter by condition grade
            if (!string.IsNullOrEmpty(conditionGrade))
            {
                query = query.Where(d =>
                    d.ConditionGrade == conditionGrade);
            }

            // Filter by damage type
            if (!string.IsNullOrEmpty(damageType))
            {
                query = query.Where(d =>
                    d.DamageType == damageType);
            }

            var devices = await query
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            // Category filter
            ViewBag.Categories = new SelectList(
                await _context.DeviceCategories
                    .OrderBy(c => c.Name)
                    .ToListAsync(),
                "Id",
                "Name",
                categoryId);

            // Condition filter
            ViewBag.ConditionGrades = await _context.Devices
                .Where(d =>
                    d.Status == "Listed" &&
                    !d.IsDeleted)
                .Select(d => d.ConditionGrade)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            // Damage filter
            ViewBag.DamageTypes = await _context.Devices
                .Where(d =>
                    d.Status == "Listed" &&
                    !d.IsDeleted)
                .Select(d => d.DamageType)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

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

            // Increase view count
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
            // Refurbished-device workflow will be connected
            // after the RepairShop repair process is implemented.

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
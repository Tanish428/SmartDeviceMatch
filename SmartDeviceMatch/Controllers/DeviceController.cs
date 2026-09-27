using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartDeviceMatch.Data;
using SmartDeviceMatch.Models;

namespace SmartDeviceMatch.Controllers
{
    [Authorize(Roles = "DeviceOwner")]
    public class DeviceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public DeviceController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Device/Index
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
                return Challenge();

            var user = await _context.AppUsers
                .FirstOrDefaultAsync(x => x.IdentityUserId == userId);

            if (user == null)
                return RedirectToAction("Create", "Profile");

            var devices = await _context.Devices
                .Include(d => d.Category)
                .Where(d => d.OwnerId == user.Id && !d.IsDeleted)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            return View(devices);
        }

        // GET: Device/Create
        public IActionResult Create()
        {
            ViewBag.CategoryId = new SelectList(
                _context.DeviceCategories,
                "Id",
                "Name");

            return View();
        }

        // POST: Device/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Device device)
        {
            var userId = _userManager.GetUserId(User);

            var user = await _context.AppUsers
                .FirstOrDefaultAsync(x => x.IdentityUserId == userId);

            if (user == null)
                return RedirectToAction("Create", "Profile");

            device.OwnerId = user.Id;
            device.Status = "Listed";

            if (ModelState.IsValid)
            {
                _context.Devices.Add(device);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            ViewBag.CategoryId = new SelectList(
                _context.DeviceCategories,
                "Id",
                "Name",
                device.CategoryId);

            return View(device);
        }
    }
}
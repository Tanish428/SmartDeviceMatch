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
        private readonly IWebHostEnvironment _environment;

        public DeviceController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
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
        public async Task<IActionResult> Create(
            Device device,
            List<IFormFile> images)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
                return Challenge();

            var user = await _context.AppUsers
                .FirstOrDefaultAsync(x => x.IdentityUserId == userId);

            if (user == null)
                return RedirectToAction("Create", "Profile");

            device.OwnerId = user.Id;
            device.Status = "Listed";

            if (!ModelState.IsValid)
            {
                ViewBag.CategoryId = new SelectList(
                    _context.DeviceCategories,
                    "Id",
                    "Name",
                    device.CategoryId);

                return View(device);
            }

            // Save device first to get Device.Id
            _context.Devices.Add(device);
            await _context.SaveChangesAsync();

            // Create wwwroot/images/devices folder path
            var uploadFolder = Path.Combine(
                _environment.WebRootPath,
                "images",
                "devices");

            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            // Save uploaded images
            if (images != null && images.Count > 0)
            {
                int displayOrder = 0;

                foreach (var image in images)
                {
                    if (image == null || image.Length == 0)
                        continue;

                    // Get file extension
                    var extension = Path.GetExtension(image.FileName)
                        .ToLowerInvariant();

                    // Allow common image formats only
                    var allowedExtensions = new[]
                    {
                        ".jpg",
                        ".jpeg",
                        ".png",
                        ".gif",
                        ".webp"
                    };

                    if (!allowedExtensions.Contains(extension))
                        continue;

                    // Generate unique file name
                    var fileName =
                        $"{Guid.NewGuid()}{extension}";

                    // Physical file path
                    var filePath = Path.Combine(
                        uploadFolder,
                        fileName);

                    // Save physical file
                    using (var stream = new FileStream(
                        filePath,
                        FileMode.Create))
                    {
                        await image.CopyToAsync(stream);
                    }

                    // URL stored in database
                    var imageUrl =
                        $"/images/devices/{fileName}";

                    // Create DeviceImage record
                    var deviceImage = new DeviceImage
                    {
                        DeviceId = device.Id,
                        ImageUrl = imageUrl,
                        DisplayOrder = displayOrder,
                        IsPrimary = displayOrder == 0
                    };

                    _context.DeviceImages.Add(deviceImage);

                    displayOrder++;
                }

                // Save DeviceImage records
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }
    }
}
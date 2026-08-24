using Microsoft.AspNetCore.Mvc;
using SmartDeviceMatch.Web.Models;
using SmartDeviceMatch.Web.Repositories;

namespace SmartDeviceMatch.Web.Controllers
{
    public class DeviceCategoryController : Controller
    {
        private readonly IDeviceCategoryRepository _repository;

        // 1. Inject the repository via the constructor
        public DeviceCategoryController(IDeviceCategoryRepository repository)
        {
            _repository = repository;
        }

        // 2. GET: /DeviceCategory/
        // Fetches all categories and passes them to the Index view
        public async Task<IActionResult> Index()
        {
            var categories = await _repository.GetAllAsync();
            return View(categories);
        }

        // 3. GET: /DeviceCategory/Create
        // Shows the empty form to the user
        public IActionResult Create()
        {
            return View();
        }

        // 4. POST: /DeviceCategory/Create
        // Receives the submitted form data and saves it to the database
        [HttpPost]
        [ValidateAntiForgeryToken] // Protects against CSRF attacks
        public async Task<IActionResult> Create(DeviceCategory category)
        {
            if (ModelState.IsValid)
            {
                await _repository.AddAsync(category);
                return RedirectToAction(nameof(Index)); // Redirect back to the list
            }

            // If validation fails (e.g., name is too short), return the form with error messages
            return View(category);
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using SmartDeviceMatch.Data;
using SmartDeviceMatch.Models;

namespace SmartDeviceMatch.Controllers
{
    public class DeviceCategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        
        public DeviceCategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

       
        public IActionResult Index()
        {
            
            var categoriesList = _context.DeviceCategories.ToList();

            
            return View(categoriesList);
        }

       
        public IActionResult Create()
        {
            return View();
        }

        
        [HttpPost]
        public IActionResult Create(DeviceCategory newCategory)
        {
            
            if (ModelState.IsValid)
            {
                _context.DeviceCategories.Add(newCategory);
                _context.SaveChanges();                   

                return RedirectToAction("Index");         
            }

           
            return View(newCategory);
        }
    }
}
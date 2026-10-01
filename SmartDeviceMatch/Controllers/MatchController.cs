using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartDeviceMatch.Data;
using SmartDeviceMatch.Services;
using SmartDeviceMatch.ViewModels;

namespace SmartDeviceMatch.Controllers
{
    [Authorize]
    public class MatchController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMatchingService _matchingService;

        public MatchController(
            ApplicationDbContext context,
            IMatchingService matchingService)
        {
            _context = context;
            _matchingService = matchingService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int deviceId)
        {
            var device = await _context.Devices
                .Include(d => d.Category)
                .FirstOrDefaultAsync(d => d.Id == deviceId);

            if (device == null)
            {
                return NotFound();
            }

            var matches = await _matchingService
                .FindMatchesAsync(deviceId);

            var matchIds = matches
                .Select(m => m.Id)
                .ToList();

            var detailedMatches = await _context.Matches
                .Include(m => m.Shop)
                .Where(m => matchIds.Contains(m.Id))
                .ToListAsync();

            var viewModels = detailedMatches
                .OrderByDescending(m => m.CompatibilityScore)
                .ThenBy(m => m.DistanceKm)
                .Select(m => new MatchViewModel
                {
                    Id = m.Id,
                    DeviceId = m.DeviceId,
                    ShopId = m.ShopId,

                    ShopName = m.Shop?.ShopName ?? "Unknown Shop",
                    City = m.Shop?.City ?? "",
                    State = m.Shop?.State ?? "",

                    Rating = m.Shop?.Rating ?? 0,
                    YearsOfExperience =
                        m.Shop?.YearsOfExperience ?? 0,

                    CompatibilityScore = m.CompatibilityScore,

                    CategoryScore = m.CategoryScore,
                    BrandScore = m.BrandScore,
                    ProximityScore = m.ProximityScore,
                    ExpertiseScore = m.ExpertiseScore,
                    RatingScore = m.RatingScore,

                    DistanceKm = m.DistanceKm
                })
                .ToList();

            return View(viewModels);
        }
    }
}
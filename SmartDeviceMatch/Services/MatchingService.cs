using Microsoft.EntityFrameworkCore;
using SmartDeviceMatch.Data;
using SmartDeviceMatch.Models;

namespace SmartDeviceMatch.Services
{
    public class MatchingService : IMatchingService
    {
        private readonly ApplicationDbContext _context;

        public MatchingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Match>> FindMatchesAsync(int deviceId)
        {
            var device = await _context.Devices
                .Include(d => d.Category)
                .FirstOrDefaultAsync(d => d.Id == deviceId);

            if (device == null)
            {
                return new List<Match>();
            }

            var shops = await _context.RepairShops
                .Include(s => s.User)
                .Include(s => s.ShopSpecializations)
                    .ThenInclude(ss => ss.Category)
                .Where(s => s.IsVerified)
                .ToListAsync();

            var matches = new List<Match>();

            foreach (var shop in shops)
            {
                var specializations = shop.ShopSpecializations
                    .Where(s => s.CategoryId == device.CategoryId)
                    .ToList();

                if (!specializations.Any())
                {
                    continue;
                }

                var bestSpecialization = specializations
                    .OrderByDescending(s => s.ExpertiseLevel)
                    .First();

                int categoryScore = CalculateCategoryScore(
                    device.CategoryId,
                    bestSpecialization.CategoryId);

                int brandScore = CalculateBrandScore(
                    device.BrandName,
                    bestSpecialization.BrandName);

                double distanceKm = CalculateDistance(
                    device.Latitude,
                    device.Longitude,
                    shop.Latitude,
                    shop.Longitude);

                int proximityScore = CalculateProximityScore(distanceKm);

                int expertiseScore = CalculateExpertiseScore(
                    bestSpecialization.ExpertiseLevel);

                int ratingScore = CalculateRatingScore(shop.Rating);

                int totalScore =
                    categoryScore +
                    brandScore +
                    proximityScore +
                    expertiseScore +
                    ratingScore;

                if (totalScore < 60)
                {
                    continue;
                }

                matches.Add(new Match
                {
                    DeviceId = device.Id,
                    ShopId = shop.Id,

                    CompatibilityScore = totalScore,

                    CategoryScore = categoryScore,
                    BrandScore = brandScore,
                    ProximityScore = proximityScore,
                    ExpertiseScore = expertiseScore,
                    RatingScore = ratingScore,

                    DistanceKm = Math.Round(distanceKm, 2),

                    Status = "Active",
                    CreatedAt = DateTime.UtcNow
                });
            }

            var oldMatches = await _context.Matches
                .Where(m => m.DeviceId == deviceId && m.Status == "Active")
                .ToListAsync();

            foreach (var oldMatch in oldMatches)
            {
                oldMatch.Status = "Expired";
            }

            if (matches.Any())
            {
                await _context.Matches.AddRangeAsync(matches);
                await _context.SaveChangesAsync();
            }

            return matches
                .OrderByDescending(m => m.CompatibilityScore)
                .ThenBy(m => m.DistanceKm)
                .ToList();
        }

        private static int CalculateCategoryScore(
            int deviceCategoryId,
            int specializationCategoryId)
        {
            return deviceCategoryId == specializationCategoryId
                ? 30
                : 0;
        }

        private static int CalculateBrandScore(
            string deviceBrand,
            string? specializationBrand)
        {
            if (string.IsNullOrWhiteSpace(specializationBrand))
            {
                return 0;
            }

            if (string.IsNullOrWhiteSpace(deviceBrand))
            {
                return 0;
            }

            return string.Equals(
                deviceBrand.Trim(),
                specializationBrand.Trim(),
                StringComparison.OrdinalIgnoreCase)
                ? 25
                : 0;
        }

        private static int CalculateProximityScore(double distanceKm)
        {
            if (distanceKm <= 5)
                return 20;

            if (distanceKm <= 10)
                return 16;

            if (distanceKm <= 25)
                return 12;

            if (distanceKm <= 50)
                return 8;

            if (distanceKm <= 100)
                return 4;

            return 0;
        }

        private static int CalculateExpertiseScore(int expertiseLevel)
        {
            return Math.Clamp(expertiseLevel, 1, 5) * 3;
        }

        private static int CalculateRatingScore(double rating)
        {
            if (rating >= 4.5)
                return 10;

            if (rating >= 4.0)
                return 8;

            if (rating >= 3.0)
                return 6;

            if (rating >= 2.0)
                return 4;

            if (rating >= 1.0)
                return 2;

            return 0;
        }

        private static double CalculateDistance(
            double latitude1,
            double longitude1,
            double latitude2,
            double longitude2)
        {
            const double earthRadiusKm = 6371.0;

            double lat1 = DegreesToRadians(latitude1);
            double lat2 = DegreesToRadians(latitude2);

            double deltaLat =
                DegreesToRadians(latitude2 - latitude1);

            double deltaLon =
                DegreesToRadians(longitude2 - longitude1);

            double a =
                Math.Sin(deltaLat / 2) *
                Math.Sin(deltaLat / 2) +
                Math.Cos(lat1) *
                Math.Cos(lat2) *
                Math.Sin(deltaLon / 2) *
                Math.Sin(deltaLon / 2);

            double c =
                2 * Math.Atan2(
                    Math.Sqrt(a),
                    Math.Sqrt(1 - a));

            return earthRadiusKm * c;
        }

        private static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }
    }
}
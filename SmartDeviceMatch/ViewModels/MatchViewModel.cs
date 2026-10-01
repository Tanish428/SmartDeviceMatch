namespace SmartDeviceMatch.ViewModels
{
    public class MatchViewModel
    {
        public int Id { get; set; }

        public int DeviceId { get; set; }

        public int ShopId { get; set; }

        public string ShopName { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;

        public string State { get; set; } = string.Empty;

        public double Rating { get; set; }

        public int YearsOfExperience { get; set; }

        public int CompatibilityScore { get; set; }

        public int CategoryScore { get; set; }

        public int BrandScore { get; set; }

        public int ProximityScore { get; set; }

        public int ExpertiseScore { get; set; }

        public int RatingScore { get; set; }

        public double DistanceKm { get; set; }

        public int ExpertiseLevel { get; set; }

        public string? BrandName { get; set; }
    }
}
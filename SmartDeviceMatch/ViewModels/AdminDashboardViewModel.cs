
using SmartDeviceMatch.Models;

namespace SmartDeviceMatch.ViewModels
{
    public class AdminDashboardViewModel
    {
        // User Statistics
        public int TotalUsers { get; set; }
        public int DeviceOwners { get; set; }
        public int RepairShopUsers { get; set; }
        public int Buyers { get; set; }
        public int BannedUsers { get; set; }

        // Repair Shop Statistics
        public int TotalRepairShops { get; set; }
        public int VerifiedRepairShops { get; set; }
        public int PendingRepairShops { get; set; }

        // Device Statistics
        public int TotalDevices { get; set; }
        public int ActiveListings { get; set; }
        public int DeletedDevices { get; set; }
        public int UrgentListings { get; set; }
        public int DevicesInRepair { get; set; }
        public int RefurbishedDevices { get; set; }

        // Offer Statistics
        public int TotalOffers { get; set; }
        public int PendingOffers { get; set; }
        public int AcceptedOffers { get; set; }

        // Transaction Statistics
        public int CompletedTransactions { get; set; }
        public int EscrowedTransactions { get; set; }

        // Matching Statistics
        public int TotalMatches { get; set; }
        public int ActiveMatches { get; set; }

        // Review Statistics
        public int TotalReviews { get; set; }
        public double AverageRating { get; set; }

        // Chart Data
        public List<MonthlyCountViewModel> MonthlyListings { get; set; } = new();

        public List<StatusCountViewModel> DeviceStatuses { get; set; } = new();

        // Recent Activity
        public List<AppUser> RecentUsers { get; set; } = new();

        public List<Offer> RecentOffers { get; set; } = new();

        public List<Review> RecentReviews { get; set; } = new();
    }

    public class MonthlyCountViewModel
    {
        public string Month { get; set; } = string.Empty;

        public int Count { get; set; }
    }

    public class StatusCountViewModel
    {
        public string Status { get; set; } = string.Empty;

        public int Count { get; set; }
    }
}
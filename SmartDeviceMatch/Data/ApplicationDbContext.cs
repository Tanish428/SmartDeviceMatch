using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartDeviceMatch.Models;

namespace SmartDeviceMatch.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // User Profile
        public DbSet<AppUser> AppUsers { get; set; }

        // Marketplace Core
        public DbSet<DeviceCategory> DeviceCategories { get; set; }
        public DbSet<Device> Devices { get; set; }
        public DbSet<DeviceImage> DeviceImages { get; set; }

        // Marketplace Ecosystem
        public DbSet<Offer> Offers { get; set; }
        public DbSet<RepairShop> RepairShops { get; set; }
        public DbSet<ShopSpecialization> ShopSpecializations { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // One IdentityUser can have only one AppUser
            builder.Entity<AppUser>()
                .HasIndex(u => u.IdentityUserId)
                .IsUnique();

            // Prevent multiple cascade paths
            foreach (var relationship in builder.Model
                .GetEntityTypes()
                .SelectMany(e => e.GetForeignKeys()))
            {
                relationship.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }
    }
}
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartDeviceMatch.Models;

namespace SmartDeviceMatch.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<AppUser> AppUsers { get; set; }
        public DbSet<DeviceCategory> DeviceCategories { get; set; }
        public DbSet<Device> Devices { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // This guarantees the strict 1-to-1 relationship. 
            // It ensures one login account cannot create multiple AppUser profiles.
            builder.Entity<AppUser>()
                .HasIndex(u => u.IdentityUserId)
                .IsUnique();

            // Prevents the "multiple cascade paths" error during deletion
            foreach (var relationship in builder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            {
                relationship.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }
    }
}
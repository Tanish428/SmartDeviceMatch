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

        // ==========================================
        // User Profile
        // ==========================================

        public DbSet<AppUser> AppUsers { get; set; }


        // ==========================================
        // Marketplace Core
        // ==========================================

        public DbSet<DeviceCategory> DeviceCategories { get; set; }

        public DbSet<Device> Devices { get; set; }

        public DbSet<DeviceImage> DeviceImages { get; set; }


        // ==========================================
        // Marketplace Ecosystem
        // ==========================================

        public DbSet<Offer> Offers { get; set; }

        public DbSet<RepairShop> RepairShops { get; set; }

        public DbSet<ShopSpecialization> ShopSpecializations { get; set; }

        public DbSet<Review> Reviews { get; set; }

        public DbSet<Notification> Notifications { get; set; }


        // ==========================================
        // Chat
        // ==========================================

        public DbSet<ChatMessage> ChatMessages { get; set; }

        public DbSet<Match> Matches { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);


            // ==========================================
            // AppUser - IdentityUser
            // ==========================================

            builder.Entity<AppUser>()
                .HasIndex(u => u.IdentityUserId)
                .IsUnique();


            // ==========================================
            // Offer -> Buyer
            // ==========================================

            builder.Entity<Offer>()
                .HasOne(o => o.Buyer)
                .WithMany()
                .HasForeignKey(o => o.BuyerId)
                .OnDelete(DeleteBehavior.Restrict);


            // ==========================================
            // Notification -> AppUser
            // ==========================================

            builder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Restrict);


            // ==========================================
            // RepairShop -> AppUser
            // ==========================================

            builder.Entity<RepairShop>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);


            // ==========================================
            // Review -> Reviewer
            // ==========================================

            builder.Entity<Review>()
                .HasOne(r => r.Reviewer)
                .WithMany()
                .HasForeignKey(r => r.ReviewerId)
                .OnDelete(DeleteBehavior.Restrict);


            // ==========================================
            // Review -> Reviewee
            // ==========================================

            builder.Entity<Review>()
                .HasOne(r => r.Reviewee)
                .WithMany()
                .HasForeignKey(r => r.RevieweeId)
                .OnDelete(DeleteBehavior.Restrict);


            // ==========================================
            // ChatMessage -> Sender
            // ==========================================

            builder.Entity<ChatMessage>()
                .HasOne(c => c.Sender)
                .WithMany()
                .HasForeignKey(c => c.SenderId)
                .OnDelete(DeleteBehavior.Restrict);


            // ==========================================
            // ChatMessage -> Receiver
            // ==========================================

            builder.Entity<ChatMessage>()
                .HasOne(c => c.Receiver)
                .WithMany()
                .HasForeignKey(c => c.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);


            // ==========================================
            // ChatMessage -> Device
            // ==========================================

            builder.Entity<ChatMessage>()
                .HasOne(c => c.Device)
                .WithMany()
                .HasForeignKey(c => c.DeviceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Match>()
                 .HasOne(m => m.Device)
                 .WithMany()
                 .HasForeignKey(m => m.DeviceId)
                 .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Match>()
                .HasOne(m => m.Shop)
                .WithMany()
                .HasForeignKey(m => m.ShopId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ShopSpecialization>()
                .HasOne(s => s.Shop)
                .WithMany(r => r.ShopSpecializations)
                .HasForeignKey(s => s.ShopId)
                .OnDelete(DeleteBehavior.Restrict);

            // ==========================================
            // Prevent cascade paths
            // ==========================================

            foreach (var relationship in builder.Model
                .GetEntityTypes()
                .SelectMany(e => e.GetForeignKeys()))
            {
                relationship.DeleteBehavior =
                    DeleteBehavior.Restrict;
            }
        }
    }
}
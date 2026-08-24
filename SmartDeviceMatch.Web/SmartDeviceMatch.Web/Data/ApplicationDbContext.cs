using Microsoft.EntityFrameworkCore;
using SmartDeviceMatch.Web.Models;

namespace SmartDeviceMatch.Web.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<DeviceCategory> DeviceCategories { get; set; }
    }
}
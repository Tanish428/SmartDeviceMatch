using Microsoft.EntityFrameworkCore;
using SmartDeviceMatch.Web.Data;
using SmartDeviceMatch.Web.Models;

namespace SmartDeviceMatch.Web.Repositories
{
    public class DeviceCategoryRepository : IDeviceCategoryRepository
    {
        private readonly ApplicationDbContext _context;

        public DeviceCategoryRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<DeviceCategory>> GetAllAsync()
        {
            return await _context.DeviceCategories.ToListAsync();
        }

        public async Task<DeviceCategory?> GetByIdAsync(int id)
        {
            return await _context.DeviceCategories.FindAsync(id);
        }

        public async Task AddAsync(DeviceCategory category)
        {
            await _context.DeviceCategories.AddAsync(category);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(DeviceCategory category)
        {
            _context.DeviceCategories.Update(category);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var category = await _context.DeviceCategories.FindAsync(id);
            if (category != null)
            {
                _context.DeviceCategories.Remove(category);
                await _context.SaveChangesAsync();
            }
        }
    }
}
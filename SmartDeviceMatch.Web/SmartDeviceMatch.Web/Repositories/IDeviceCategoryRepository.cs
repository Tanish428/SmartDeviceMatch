using SmartDeviceMatch.Web.Models;

namespace SmartDeviceMatch.Web.Repositories
{
    public interface IDeviceCategoryRepository
    {
        Task<IEnumerable<DeviceCategory>> GetAllAsync();
        Task<DeviceCategory?> GetByIdAsync(int id);
        Task AddAsync(DeviceCategory category);
        Task UpdateAsync(DeviceCategory category);
        Task DeleteAsync(int id);
    }
}
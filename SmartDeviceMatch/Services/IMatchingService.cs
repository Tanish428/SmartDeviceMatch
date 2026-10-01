using SmartDeviceMatch.Models;

namespace SmartDeviceMatch.Services
{
    public interface IMatchingService
    {
        Task<List<Match>> FindMatchesAsync(int deviceId);
    }
}
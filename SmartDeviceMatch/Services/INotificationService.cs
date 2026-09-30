namespace SmartDeviceMatch.Services
{
    public interface INotificationService
    {
        Task CreateAsync(
            int userId,
            string title,
            string message,
            string type,
            string? referenceId = null,
            string? referenceType = null);
    }
}
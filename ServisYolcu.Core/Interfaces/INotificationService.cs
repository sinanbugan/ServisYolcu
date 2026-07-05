using ServisYolcu.Core.DTOs.Notification;

namespace ServisYolcu.Core.Interfaces;

public interface INotificationService
{
    Task RegisterDeviceTokenAsync(int userId, string deviceToken, string? platform = null);
    Task<NotificationDeliveryResultDto> SendPersonalNotificationAsync(int userId, string title, string body, Dictionary<string, string>? data = null, CancellationToken cancellationToken = default);
    Task<NotificationDeliveryResultDto> SendMultiNotificationAsync(IEnumerable<int> userIds, string title, string body, Dictionary<string, string>? data = null, CancellationToken cancellationToken = default);
    Task<NotificationDeliveryResultDto> SendBulkNotificationAsync(string title, string body, Dictionary<string, string>? data = null, int? companyId = null, CancellationToken cancellationToken = default);
    Task<NotificationDeliveryResultDto> SendTripStartedNotificationAsync(int tripId, string title, string body, Dictionary<string, string>? data = null, CancellationToken cancellationToken = default);
}

namespace ServisYolcu.Core.DTOs.Notification;

public class RegisterDeviceTokenDto
{
    public string DeviceToken { get; set; } = string.Empty;
    public string? Platform { get; set; }
}

namespace ServisYolcu.Core.DTOs.Notification;

public class SendNotificationDto
{
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public Dictionary<string, string>? Data { get; set; }
}

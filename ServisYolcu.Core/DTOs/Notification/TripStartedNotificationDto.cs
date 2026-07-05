namespace ServisYolcu.Core.DTOs.Notification;

public class TripStartedNotificationDto
{
    public int TripId { get; set; }
    public string Title { get; set; } = "Yolculuk başladı";
    public string Body { get; set; } = "Yolculuğunuz başladı.";
    public Dictionary<string, string>? Data { get; set; }
}

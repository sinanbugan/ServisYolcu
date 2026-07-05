namespace ServisYolcu.Core.Entities;

public class NotificationLog
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int? UserId { get; set; }
    public int? CompanyId { get; set; }
    public int SentCount { get; set; }
    public int FailedCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

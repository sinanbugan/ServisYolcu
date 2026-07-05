namespace ServisYolcu.Core.DTOs.Notification;

public class NotificationDeliveryResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int SentCount { get; set; }
    public int FailedCount { get; set; }
    // Summary of error types and their counts (returned to clients)
    public Dictionary<string, int> ErrorSummary { get; set; } = new();

    // Optional: id of the persisted NotificationLog for fetching full details by admins
    public int? LogId { get; set; }

    // Detailed per-token results are stored in DB and not returned to mobile clients.
    public List<NotificationTokenResultDto> TokenResults { get; set; } = new();
}

public class NotificationTokenResultDto
{
    public string Token { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

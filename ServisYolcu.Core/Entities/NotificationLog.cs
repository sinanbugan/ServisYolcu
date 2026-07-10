namespace ServisYolcu.Core.Entities;

public class NotificationLog
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int? UserId { get; set; }
    public int? CompanyId { get; set; }

    /// <summary>Sefere bağlı bildirimlerde (trip_started, return_reminder) hangi sefer olduğu.</summary>
    public int? TripId { get; set; }

    /// <summary>
    /// Yalnızca dönüş hatırlatmalarında dolu. (Type, TripId, ReferenceDate) üzerindeki filtreli
    /// unique index ile birlikte, aynı sefer/gün için ikinci bir hatırlatmanın gönderilmesini
    /// veritabanı seviyesinde engeller — birden fazla sunucu örneği çalışsa bile.
    /// </summary>
    public DateOnly? ReferenceDate { get; set; }

    public int SentCount { get; set; }
    public int FailedCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

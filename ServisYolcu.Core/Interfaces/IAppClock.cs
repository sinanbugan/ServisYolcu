namespace ServisYolcu.Core.Interfaces;

/// <summary>
/// Sunucu UTC'de, kullanıcılar Türkiye saatinde yaşıyor. Gün sınırına dayanan her karar
/// (bugünün manifestosu, dönüş kararının geçmişte kalıp kalmadığı, hatırlatma saati)
/// yerel takvime göre verilmelidir. UtcNow.Date kullanmak gece yarısı civarında bir gün
/// kaydırır — bu soyutlama o hatayı tek yerde toplar.
/// </summary>
public interface IAppClock
{
    TimeZoneInfo TimeZone { get; }

    DateTimeOffset UtcNow { get; }

    /// <summary>Yapılandırılmış zaman diliminde şu an.</summary>
    DateTimeOffset LocalNow { get; }

    /// <summary>Yapılandırılmış zaman diliminde bugünün takvim günü.</summary>
    DateOnly LocalToday { get; }

    /// <summary>Verilen UTC zaman damgasının yerel takvim günü.</summary>
    DateOnly ToLocalDate(DateTime utc);
}

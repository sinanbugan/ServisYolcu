using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServisYolcu.Core.Interfaces;
using ServisYolcu.Core.Options;

namespace ServisYolcu.Infrastructure.Services;

public class AppClock : IAppClock
{
    public TimeZoneInfo TimeZone { get; }

    public AppClock(IOptions<ReturnPlanOptions> options, ILogger<AppClock> logger)
    {
        var id = options.Value.TimeZone;
        try
        {
            TimeZone = TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            // UTC'ye düşmek gün sınırlarını kaydırır; sessizce yapmıyoruz.
            logger.LogError(ex, "Zaman dilimi '{TimeZoneId}' bulunamadı. UTC kullanılıyor — gün sınırına dayalı hesaplar yanlış olabilir.", id);
            TimeZone = TimeZoneInfo.Utc;
        }
    }

    public DateTimeOffset LocalNow => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZone);

    public DateOnly LocalToday => DateOnly.FromDateTime(LocalNow.DateTime);
}

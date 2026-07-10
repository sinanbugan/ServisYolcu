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

    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public DateTimeOffset LocalNow => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZone);

    public DateOnly LocalToday => DateOnly.FromDateTime(LocalNow.DateTime);

    public DateOnly ToLocalDate(DateTime utc)
    {
        var asOffset = utc.Kind switch
        {
            DateTimeKind.Utc => new DateTimeOffset(utc, TimeSpan.Zero),
            DateTimeKind.Local => new DateTimeOffset(utc),
            _ => new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc), TimeSpan.Zero),
        };

        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(asOffset, TimeZone).DateTime);
    }
}

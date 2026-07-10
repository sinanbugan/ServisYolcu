using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ServisYolcu.Core.Enums;
using ServisYolcu.Core.Interfaces;
using ServisYolcu.Core.Options;
using ServisYolcu.Infrastructure.Data;

namespace ServisYolcu.API.BackgroundServices;

/// <summary>
/// Yumuşak cutoff. Yapılandırılmış yerel saatte (varsayılan 15:00, Europe/Istanbul), o gün
/// için hâlâ "Planlı" — yani onaylamamış — yolculara tek bir hatırlatma bildirimi gönderir.
/// Bildirim gittikten sonra yolcu kararını hâlâ değiştirebilir; liste dondurulmaz.
///
/// Tekrar göndermeyi NotificationService.SendReturnReminderAsync içindeki NotificationLog
/// kontrolü engeller, bu yüzden servis yeniden başlasa bile aynı gün ikinci bildirim çıkmaz.
/// </summary>
public class ReturnReminderService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReturnReminderService> _logger;
    private readonly ReturnPlanOptions _options;

    public ReturnReminderService(
        IServiceScopeFactory scopeFactory,
        ILogger<ReturnReminderService> logger,
        IOptions<ReturnPlanOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.ReminderEnabled)
        {
            _logger.LogInformation("Dönüş hatırlatma servisi kapalı.");
            return;
        }

        if (!TimeOnly.TryParse(_options.ReminderLocalTime, out var reminderTime))
        {
            _logger.LogError("ReturnPlan:ReminderLocalTime ('{Value}') okunamadı. Hatırlatma servisi çalışmayacak.", _options.ReminderLocalTime);
            return;
        }

        var interval = TimeSpan.FromMinutes(Math.Max(1, _options.PollIntervalMinutes));
        var window = TimeSpan.FromMinutes(Math.Max(1, _options.ReminderWindowMinutes));

        using var timer = new PeriodicTimer(interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(reminderTime, window, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Tek bir turun hatası servisi düşürmemeli.
                _logger.LogError(ex, "Dönüş hatırlatma turu başarısız oldu.");
            }

            if (!await timer.WaitForNextTickAsync(stoppingToken)) break;
        }
    }

    private async Task RunOnceAsync(TimeOnly reminderTime, TimeSpan window, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var clock = scope.ServiceProvider.GetRequiredService<IAppClock>();

        var localNow = clock.LocalNow.DateTime;

        // Pencere gece yarısını aşabilir (ör. hatırlatma 23:30 + 120dk). Bu yüzden çıplak
        // TimeOnly farkı yerine tam tarih çapasından ölçüyoruz: bugünün çapası henüz gelmediyse
        // dünün çapasına bakıyoruz, böylece 00:10'daki bir tur hâlâ dün 23:30'un penceresinde.
        var anchor = localNow.Date + reminderTime.ToTimeSpan();
        if (localNow < anchor)
            anchor = anchor.AddDays(-1);

        var sinceReminder = localNow - anchor;
        if (sinceReminder > window)
            return;

        // Hatırlatma hangi günün seferleri için: çapanın günü.
        var today = DateOnly.FromDateTime(anchor);

        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var returnTripIds = await context.Trips
            .Where(t => t.IsActive && t.Direction == TripDirection.Return)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        foreach (var tripId in returnTripIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await notifications.SendReturnReminderAsync(tripId, today, cancellationToken);
            if (result.SentCount > 0)
            {
                _logger.LogInformation(
                    "Dönüş hatırlatması gönderildi. Sefer={TripId} Gün={Date} Gönderilen={Sent} Başarısız={Failed}",
                    tripId, today, result.SentCount, result.FailedCount);
            }
        }
    }
}

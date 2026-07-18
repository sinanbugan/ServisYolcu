using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using ServisYolcu.Core.DTOs.Notification;
using ServisYolcu.Core.Entities;
using ServisYolcu.Core.Enums;
using ServisYolcu.Core.Interfaces;
using ServisYolcu.Core.Utilities;
using ServisYolcu.Infrastructure.Data;

namespace ServisYolcu.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private const string ReturnReminderType = "return_reminder";

    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IAppClock _clock;

    public NotificationService(AppDbContext context, IConfiguration configuration, IAppClock clock)
    {
        _context = context;
        _configuration = configuration;
        _clock = clock;
        EnsureFirebaseInitialized();
    }

    public async Task RegisterDeviceTokenAsync(int userId, string deviceToken, string? platform = null)
    {
        if (string.IsNullOrWhiteSpace(deviceToken))
            throw new ArgumentException("Device token is required.", nameof(deviceToken));

        var existing = await _context.DeviceTokens
            .FirstOrDefaultAsync(t => t.Token == deviceToken);

        if (existing is null)
        {
            _context.DeviceTokens.Add(new DeviceToken
            {
                UserId = userId,
                Token = deviceToken,
                Platform = platform,
                IsActive = true
            });
        }
        else
        {
            existing.UserId = userId;
            existing.Platform = platform;
            existing.IsActive = true;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<NotificationDeliveryResultDto> SendPersonalNotificationAsync(int userId, string title, string body, Dictionary<string, string>? data = null, CancellationToken cancellationToken = default)
    {
        var tokens = await _context.DeviceTokens
            .Where(t => t.UserId == userId && t.IsActive)
            .Select(t => t.Token)
            .ToListAsync(cancellationToken);

        return await SendToTokensAsync(tokens, title, body, data, "personal", userId, null, cancellationToken);
    }

    public async Task<NotificationDeliveryResultDto> SendMultiNotificationAsync(IEnumerable<int> userIds, string title, string body, Dictionary<string, string>? data = null, CancellationToken cancellationToken = default)
    {
        var ids = userIds.Distinct().ToList();
        var tokens = await _context.DeviceTokens
            .Where(t => ids.Contains(t.UserId) && t.IsActive)
            .Select(t => t.Token)
            .ToListAsync(cancellationToken);

        return await SendToTokensAsync(tokens, title, body, data, "multi", null, null, cancellationToken);
    }

    public async Task<NotificationDeliveryResultDto> SendBulkNotificationAsync(string title, string body, Dictionary<string, string>? data = null, int? companyId = null, CancellationToken cancellationToken = default)
    {
        var tokens = await _context.DeviceTokens
            .Where(t => t.IsActive && (!companyId.HasValue || t.User != null && t.User.CompanyId == companyId.Value))
            .Select(t => t.Token)
            .ToListAsync(cancellationToken);

        return await SendToTokensAsync(tokens, title, body, data, "bulk", null, companyId, cancellationToken);
    }

    public async Task<NotificationDeliveryResultDto> SendTripStartedNotificationAsync(int tripId, string title, string body, Dictionary<string, string>? data = null, CancellationToken cancellationToken = default)
    {
        var trip = await _context.Trips
            .FirstOrDefaultAsync(t => t.Id == tripId, cancellationToken);

        if (trip is null)
        {
            return new NotificationDeliveryResultDto
            {
                Success = false,
                Message = "Trip not found.",
                SentCount = 0,
                FailedCount = 0
            };
        }

        // Gün, sunucunun UTC tarihi değil yolcunun yerel takvim günü olmalı.
        var queryDate = _clock.LocalToday;

        var notifiedPassengerIds = trip.Direction == TripDirection.Return
            ? await GetReturnPassengerIdsAsync(tripId, queryDate, s => s != ReturnAttendanceState.NotComing, cancellationToken)
            : await GetOutboundPassengerIdsAsync(tripId, queryDate, cancellationToken);

        var tokens = await _context.DeviceTokens
            .Where(t => t.IsActive && notifiedPassengerIds.Contains(t.UserId))
            .Select(t => t.Token)
            .ToListAsync(cancellationToken);

        return await SendToTokensAsync(tokens, title, body, data, "trip_started", null, null, cancellationToken, tripId);
    }

    public async Task<NotificationDeliveryResultDto> SendReturnReminderAsync(int tripId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var trip = await _context.Trips
            .Include(t => t.Route)
            .FirstOrDefaultAsync(t => t.Id == tripId, cancellationToken);

        if (trip is null || trip.Direction != TripDirection.Return)
        {
            return new NotificationDeliveryResultDto
            {
                Success = false,
                Message = "Return trip not found.",
                SentCount = 0,
                FailedCount = 0
            };
        }

        var undecided = await GetReturnPassengerIdsAsync(
            tripId, date, s => s == ReturnAttendanceState.Planned, cancellationToken);

        if (undecided.Count == 0)
        {
            return new NotificationDeliveryResultDto
            {
                Success = true,
                Message = "No undecided passengers.",
                SentCount = 0,
                FailedCount = 0
            };
        }

        var title = "Dönüş servisi";
        var body = $"Bugün {trip.Route.Name} ile dönüyor musunuz? Lütfen onaylayın.";

        // Göndermeden ÖNCE kaydı yaz. (Type, TripId, ReferenceDate) üzerindeki filtreli unique
        // index sayesinde aynı sefer/gün için ikinci bir çağrı — ister sonraki tur, ister başka
        // bir sunucu örneği olsun — buraya çarpar ve bildirim tekrar gönderilmez.
        var log = new NotificationLog
        {
            Title = title,
            Body = body,
            Type = ReturnReminderType,
            TripId = tripId,
            ReferenceDate = date,
            SentCount = 0,
            FailedCount = 0,
        };

        _context.NotificationLogs.Add(log);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            _context.Entry(log).State = EntityState.Detached;
            return new NotificationDeliveryResultDto
            {
                Success = true,
                Message = "Reminder already sent for this trip and day.",
                SentCount = 0,
                FailedCount = 0
            };
        }

        var tokens = await _context.DeviceTokens
            .Where(t => t.IsActive && undecided.Contains(t.UserId))
            .Select(t => t.Token)
            .ToListAsync(cancellationToken);

        var data = new Dictionary<string, string>
        {
            ["type"] = ReturnReminderType,
            ["tripId"] = tripId.ToString(),
            ["date"] = date.ToString("yyyy-MM-dd"),
        };

        var result = await DispatchAsync(tokens, title, body, data, cancellationToken);

        log.SentCount = result.SentCount;
        log.FailedCount = result.FailedCount;
        await _context.SaveChangesAsync(cancellationToken);

        result.LogId = log.Id;
        return result;
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    private async Task<HashSet<int>> GetOutboundPassengerIdsAsync(int tripId, DateOnly date, CancellationToken cancellationToken)
    {
        var dayChoices = await _context.OutboundDayChoices
            .Where(c => c.Date == date)
            .ToListAsync(cancellationToken);

        var choiceByPassenger = dayChoices
            .GroupBy(c => c.PassengerId)
            .ToDictionary(g => g.Key, g => g.First());

        var monthlyReservations = await _context.MonthlyReservations
            .Where(m => m.TripId == tripId
                        && m.Direction == TripDirection.Outbound
                        && m.Year == date.Year
                        && m.Month == date.Month)
            .ToListAsync(cancellationToken);

        var monthlyByPassenger = monthlyReservations
            .GroupBy(m => m.PassengerId)
            .ToDictionary(g => g.Key, g => g.First());

        var regularReservations = await _context.Reservations
            .Where(r => r.TripId == tripId && r.Status == ReservationStatus.Confirmed)
            .ToListAsync(cancellationToken);

        var candidatePassengerIds = regularReservations.Select(r => r.PassengerId)
            .Concat(monthlyReservations.Select(m => m.PassengerId))
            .Concat(dayChoices.Where(c => c.TripId == tripId).Select(c => c.PassengerId))
            .Distinct();

        var notifiedPassengerIds = new HashSet<int>();
        foreach (var passengerId in candidatePassengerIds)
        {
            if (choiceByPassenger.TryGetValue(passengerId, out var choice))
            {
                if (choice.TripId == tripId)
                    notifiedPassengerIds.Add(passengerId);

                continue;
            }

            if (monthlyByPassenger.TryGetValue(passengerId, out var monthlyReservation))
            {
                if (!DayList.Parse(monthlyReservation.DaysOff).Contains(date.Day))
                    notifiedPassengerIds.Add(passengerId);

                continue;
            }

            notifiedPassengerIds.Add(passengerId);
        }

        return notifiedPassengerIds;
    }

    private async Task<HashSet<int>> GetReturnPassengerIdsAsync(
        int tripId, DateOnly date, Func<ReturnAttendanceState, bool> predicate, CancellationToken cancellationToken)
    {
        var roster = await ReturnRoster.BuildAsync(_context, tripId, date, cancellationToken);
        return roster.Where(r => predicate(r.State)).Select(r => r.PassengerId).ToHashSet();
    }

    private async Task<NotificationDeliveryResultDto> SendToTokensAsync(IEnumerable<string> tokens, string title, string body, Dictionary<string, string>? data, string type, int? userId, int? companyId, CancellationToken cancellationToken, int? tripId = null)
    {
        var result = await DispatchAsync(tokens, title, body, data, cancellationToken);

        var log = new NotificationLog
        {
            Title = title,
            Body = body,
            Type = type,
            UserId = userId,
            CompanyId = companyId,
            TripId = tripId,
            SentCount = result.SentCount,
            FailedCount = result.FailedCount
        };

        await _context.NotificationLogs.AddAsync(log, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        result.LogId = log.Id;
        return result;
    }

    /// <summary>
    /// FCM'e gönderir, ölü token'ları pasifleştirir ve sonucu döner. NotificationLog YAZMAZ —
    /// dönüş hatırlatması kaydı göndermeden önce yazmak zorunda olduğu için ayrıldı.
    /// </summary>
    private async Task<NotificationDeliveryResultDto> DispatchAsync(IEnumerable<string> tokens, string title, string body, Dictionary<string, string>? data, CancellationToken cancellationToken)
    {
        var tokenList = tokens.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct().ToList();
        if (tokenList.Count == 0)
        {
            return new NotificationDeliveryResultDto
            {
                Success = true,
                Message = "No active device tokens found.",
                SentCount = 0,
                FailedCount = 0
            };
        }

        var message = new MulticastMessage
        {
            Tokens = tokenList,
            Notification = new Notification
            {
                Title = title,
                Body = body
            },
            Data = data is null ? new Dictionary<string, string>() : new Dictionary<string, string>(data)
        };

        var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message, cancellationToken);

        var tokenResults = response.Responses
            .Select((sendResponse, index) => new NotificationTokenResultDto
            {
                Token = tokenList[index],
                Success = sendResponse.IsSuccess,
                ErrorMessage = sendResponse.Exception?.Message
            })
            .ToList();

        // Deactivate tokens that failed delivery
        var tokensToDeactivate = tokenResults
            .Where(tr => !tr.Success)
            .Select(tr => tr.Token)
            .ToList();

        if (tokensToDeactivate.Count > 0)
        {
            var dbTokens = await _context.DeviceTokens
                .Where(dt => tokensToDeactivate.Contains(dt.Token) && dt.IsActive)
                .ToListAsync(cancellationToken);

            if (dbTokens.Count > 0)
            {
                foreach (var dbt in dbTokens)
                {
                    dbt.IsActive = false;
                    dbt.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        // Build error summary (group by common known error keywords or raw message)
        var errorSummary = tokenResults
            .Where(tr => !tr.Success)
            .GroupBy(tr =>
            {
                var m = tr.ErrorMessage ?? "Unknown";
                if (m.Contains("NotRegistered", StringComparison.OrdinalIgnoreCase)) return "NotRegistered";
                if (m.Contains("InvalidArgument", StringComparison.OrdinalIgnoreCase)) return "InvalidArgument";
                if (m.Contains("Unavailable", StringComparison.OrdinalIgnoreCase)) return "Unavailable";
                return m;
            })
            .ToDictionary(g => g.Key, g => g.Count());

        return new NotificationDeliveryResultDto
        {
            Success = response.FailureCount == 0,
            Message = response.FailureCount == 0 ? "Notifications sent successfully." : "Some notifications failed.",
            SentCount = response.SuccessCount,
            FailedCount = response.FailureCount,
            ErrorSummary = errorSummary,
            TokenResults = new List<NotificationTokenResultDto>()
        };
    }

    private static void EnsureFirebaseInitialized()
    {
        if (FirebaseApp.DefaultInstance is not null)
            return;

        var path = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIALS_PATH");
        if (string.IsNullOrWhiteSpace(path))
            throw new InvalidOperationException("FIREBASE_CREDENTIALS_PATH is not configured.");

        FirebaseApp.Create(new AppOptions
        {
            Credential = GoogleCredential.FromFile(path)
        });
    }
}

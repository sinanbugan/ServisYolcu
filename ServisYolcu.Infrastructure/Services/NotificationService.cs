using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ServisYolcu.Core.DTOs.Notification;
using ServisYolcu.Core.Entities;
using ServisYolcu.Core.Interfaces;
using ServisYolcu.Infrastructure.Data;

namespace ServisYolcu.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public NotificationService(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
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

        var queryDate = DateTime.UtcNow.Date;
        var monthlyReservations = await _context.MonthlyReservations
            .Where(m => m.TripId == tripId && m.Year == queryDate.Year && m.Month == queryDate.Month)
            .ToListAsync(cancellationToken);

        var monthlyByPassenger = monthlyReservations
            .GroupBy(m => m.PassengerId)
            .ToDictionary(g => g.Key, g => g.First());

        var regularReservations = await _context.Reservations
            .Where(r => r.TripId == tripId && r.Status == ReservationStatus.Confirmed)
            .ToListAsync(cancellationToken);

        var notifiedPassengerIds = new HashSet<int>();

        foreach (var reservation in regularReservations)
        {
            if (ShouldNotifyPassengerForTrip(reservation.PassengerId, monthlyByPassenger, queryDate.Day))
            {
                notifiedPassengerIds.Add(reservation.PassengerId);
            }
        }

        foreach (var monthlyReservation in monthlyReservations)
        {
            if (notifiedPassengerIds.Contains(monthlyReservation.PassengerId))
                continue;

            if (ShouldNotifyPassengerForTrip(monthlyReservation.PassengerId, monthlyByPassenger, queryDate.Day))
            {
                notifiedPassengerIds.Add(monthlyReservation.PassengerId);
            }
        }

        var tokens = await _context.DeviceTokens
            .Where(t => t.IsActive && notifiedPassengerIds.Contains(t.UserId))
            .Select(t => t.Token)
            .ToListAsync(cancellationToken);

        return await SendToTokensAsync(tokens, title, body, data, "trip_started", null, null, cancellationToken);
    }

    private static bool ShouldNotifyPassengerForTrip(int passengerId, IReadOnlyDictionary<int, MonthlyReservation> monthlyByPassenger, int day)
    {
        if (!monthlyByPassenger.TryGetValue(passengerId, out var monthlyReservation))
            return true;

        return !ParseDaysOff(monthlyReservation.DaysOff).Contains(day);
    }

    private static List<int> ParseDaysOff(string csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return new List<int>();
        return csv.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s, out var value) ? value : 0)
            .Where(v => v > 0)
            .ToList();
    }

    private async Task<NotificationDeliveryResultDto> SendToTokensAsync(IEnumerable<string> tokens, string title, string body, Dictionary<string, string>? data, string type, int? userId, int? companyId, CancellationToken cancellationToken)
    {
        var tokenList = tokens.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct().ToList();
        if (tokenList.Count == 0)
        {
            await _context.NotificationLogs.AddAsync(new NotificationLog
            {
                Title = title,
                Body = body,
                Type = type,
                UserId = userId,
                CompanyId = companyId,
                SentCount = 0,
                FailedCount = 0
            }, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

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

        // Persist a NotificationLog (summary stored in columns; full tokenResults are kept only in logs if needed)
        var log = new NotificationLog
        {
            Title = title,
            Body = body,
            Type = type,
            UserId = userId,
            CompanyId = companyId,
            SentCount = response.SuccessCount,
            FailedCount = response.FailureCount
        };

        await _context.NotificationLogs.AddAsync(log, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

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
            LogId = log.Id,
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

using Microsoft.EntityFrameworkCore;
using ServisYolcu.Core.Entities;
using ServisYolcu.Core.Enums;
using ServisYolcu.Core.Utilities;
using ServisYolcu.Infrastructure.Data;

namespace ServisYolcu.Infrastructure.Services;

internal sealed record ReturnRosterRow(
    int PassengerId,
    User? Passenger,
    ReturnAttendanceState State,
    int? BoardingStopId,
    bool HasTemplate);

/// <summary>
/// Belirli bir dönüş seferinin, belirli bir gündeki yolcu listesini kurar.
/// Sürücü manifestosu (TripService) ve bildirim dağıtımı (NotificationService) buradan okur;
/// iki yerin zamanla ayrışmaması için tek kaynak.
/// </summary>
internal static class ReturnRoster
{
    public static async Task<List<ReturnRosterRow>> BuildAsync(
        AppDbContext context, int tripId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var templates = await context.MonthlyReservations
            .Include(m => m.Passenger)
            .Where(m => m.TripId == tripId
                        && m.Direction == TripDirection.Return
                        && m.Year == date.Year
                        && m.Month == date.Month)
            .ToListAsync(cancellationToken);

        var reservations = await context.Reservations
            .Include(r => r.Passenger)
            .Where(r => r.TripId == tripId && r.Status == ReservationStatus.Confirmed)
            .ToListAsync(cancellationToken);

        var basePassengerIds = templates.Select(t => t.PassengerId)
            .Concat(reservations.Select(r => r.PassengerId))
            .Distinct()
            .ToList();

        // İlgili TÜM kararlar gerekli: bu sefere geçenler (şablonu olmayan yolcular dâhil)
        // ve şablon yolcularının başka bir sefere kayması / o günü iptal etmesi.
        var choices = await context.ReturnDayChoices
            .Include(c => c.Passenger)
            .Where(c => c.Date == date
                        && (c.TripId == tripId || basePassengerIds.Contains(c.PassengerId)))
            .ToListAsync(cancellationToken);

        var templateByPassenger = templates.ToDictionary(m => m.PassengerId);
        var reservationByPassenger = reservations.ToDictionary(r => r.PassengerId);
        var choiceByPassenger = choices.ToDictionary(c => c.PassengerId);

        var passengerIds = basePassengerIds
            .Concat(choices.Where(c => c.TripId == tripId).Select(c => c.PassengerId))
            .Distinct()
            .ToList();

        var rows = new List<ReturnRosterRow>(passengerIds.Count);

        foreach (var passengerId in passengerIds)
        {
            templateByPassenger.TryGetValue(passengerId, out var template);
            reservationByPassenger.TryGetValue(passengerId, out var reservation);
            choiceByPassenger.TryGetValue(passengerId, out var choice);

            // Gidişteki gibi temel kayıt varsa o yolcuyu önce bu kayıttan kur.
            // Günlük dönüş kararı sadece yokluğu/gelmeyi değiştirsin; boarding stop'u ezmesin.
            if (choice is not null
                && choice.Decision == ReturnDecision.Coming
                && choice.TripId != tripId)
                continue;

            var coming = reservation is not null;
            if (template is not null)
                coming = !DayList.Parse(template.DaysOff).Contains(date.Day);

            var isOverrideForThisTrip = choice is not null
                                        && choice.Decision == ReturnDecision.Coming
                                        && choice.TripId == tripId;

            if (choice is not null && choice.Decision == ReturnDecision.NotComing)
                coming = false;
            if (isOverrideForThisTrip)
                coming = true;

            var baseStopId = reservation?.BoardingStopId ?? template?.BoardingStopId;
            var boardingStopId = isOverrideForThisTrip
                ? choice!.BoardingStopId ?? baseStopId
                : baseStopId;

            var state = coming ? ReturnAttendanceState.Confirmed : ReturnAttendanceState.NotComing;

            rows.Add(new ReturnRosterRow(
                passengerId,
                reservation?.Passenger ?? choice?.Passenger ?? template?.Passenger,
                state,
                boardingStopId,
                template is not null));
        }

        return rows;
    }
}

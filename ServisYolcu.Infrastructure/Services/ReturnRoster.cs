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

        var templatePassengerIds = templates.Select(m => m.PassengerId).ToList();

        // İlgili TÜM kararlar gerekli: bu sefere geçenler (şablonu olmayan yolcular dâhil)
        // ve şablon yolcularının başka bir sefere kayması / o günü iptal etmesi.
        var choices = await context.ReturnDayChoices
            .Include(c => c.Passenger)
            .Where(c => c.Date == date
                        && (c.TripId == tripId || templatePassengerIds.Contains(c.PassengerId)))
            .ToListAsync(cancellationToken);

        var templateByPassenger = templates.ToDictionary(m => m.PassengerId);
        var choiceByPassenger = choices.ToDictionary(c => c.PassengerId);

        var passengerIds = templatePassengerIds
            .Concat(choices.Where(c => c.TripId == tripId).Select(c => c.PassengerId))
            .Distinct()
            .ToList();

        var rows = new List<ReturnRosterRow>(passengerIds.Count);

        foreach (var passengerId in passengerIds)
        {
            templateByPassenger.TryGetValue(passengerId, out var template);
            choiceByPassenger.TryGetValue(passengerId, out var choice);

            var resolution = ReturnAttendanceResolver.Resolve(date, choice, template);

            // Yolcu bu manifestoya ancak çözümlenen sefer BU sefer ise "geliyor" olarak girer.
            // Başka bir dönüş seferine kaydıysa burada "gelmiyor" görünür.
            var onThisTrip = resolution.TripId == tripId && resolution.State != ReturnAttendanceState.NotComing;
            var state = onThisTrip ? resolution.State : ReturnAttendanceState.NotComing;

            // Gelmeyen şablon yolcusunu her zamanki durağında (üzeri çizili) göstermek
            // sürücü için daha okunaklı.
            var boardingStopId = onThisTrip ? resolution.BoardingStopId : template?.BoardingStopId;

            rows.Add(new ReturnRosterRow(
                passengerId,
                choice?.Passenger ?? template?.Passenger,
                state,
                boardingStopId,
                template is not null));
        }

        return rows;
    }
}

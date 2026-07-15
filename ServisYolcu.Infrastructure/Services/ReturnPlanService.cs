using Microsoft.EntityFrameworkCore;
using Npgsql;
using ServisYolcu.Core.DTOs.ReturnPlan;
using ServisYolcu.Core.Entities;
using ServisYolcu.Core.Enums;
using ServisYolcu.Core.Interfaces;
using ServisYolcu.Core.Utilities;
using ServisYolcu.Infrastructure.Data;

namespace ServisYolcu.Infrastructure.Services;

/// <summary>
/// Yolcunun dönüş planını yönetir: aylık şablon (MonthlyReservation, Direction = Return)
/// üzerine günlük kararlar (ReturnDayChoice) yazılır.
///
/// Not: Bu kod tabanında bir Trip, tek bir günün kalkışı değil, tekrarlayan bir hattır
/// (DepartureTime hattın nominal saatidir). Aylık abonelik zaten böyle çalışıyor, dönüş
/// kararları da aynı varsayımı sürdürür — bu yüzden seçilen günün trip.DepartureTime
/// tarihiyle eşleşmesi beklenmez.
/// </summary>
public class ReturnPlanService : IReturnPlanService
{
    private readonly AppDbContext _context;
    private readonly IAppClock _clock;

    public ReturnPlanService(AppDbContext context, IAppClock clock)
    {
        _context = context;
        _clock = clock;
    }

    public async Task<ReturnMonthDto> GetMonthAsync(int passengerId, int year, int month, CancellationToken cancellationToken = default)
    {
        // ExceptionMiddleware yalnızca InvalidOperationException'ı 400'e çeviriyor.
        if (month is < 1 or > 12)
            throw new InvalidOperationException("Ay 1-12 aralığında olmalıdır.");
        if (year is < 2000 or > 2100)
            throw new InvalidOperationException("Yıl geçersiz.");

        var template = await _context.MonthlyReservations
            .Include(m => m.Trip).ThenInclude(t => t.Route)
            .Include(m => m.BoardingStop)
            .FirstOrDefaultAsync(
                m => m.PassengerId == passengerId
                     && m.Direction == TripDirection.Return
                     && m.Year == year
                     && m.Month == month,
                cancellationToken);

        var firstDay = new DateOnly(year, month, 1);
        var lastDay = new DateOnly(year, month, DateTime.DaysInMonth(year, month));

        var choices = await _context.ReturnDayChoices
            .Include(c => c.Trip).ThenInclude(t => t!.Route)
            .Include(c => c.BoardingStop)
            .Where(c => c.PassengerId == passengerId && c.Date >= firstDay && c.Date <= lastDay)
            .ToListAsync(cancellationToken);

        var choiceByDate = choices.ToDictionary(c => c.Date);
        var today = _clock.LocalToday;

        var days = new List<ReturnDayDto>();
        for (var date = firstDay; date <= lastDay; date = date.AddDays(1))
        {
            choiceByDate.TryGetValue(date, out var choice);
            days.Add(BuildDayDto(date, choice, template, today));
        }

        return new ReturnMonthDto
        {
            Year = year,
            Month = month,
            Template = template is null ? null : new ReturnTemplateDto
            {
                Id = template.Id,
                TripId = template.TripId,
                RouteName = template.Trip.Route.Name,
                StartPoint = template.Trip.Route.StartPoint,
                EndPoint = template.Trip.Route.EndPoint,
                DepartureTime = template.Trip.DepartureTime,
                DaysOff = DayList.Parse(template.DaysOff),
                BoardingStopId = template.BoardingStopId,
                BoardingStopName = template.BoardingStop?.Name,
            },
            Days = days,
        };
    }

    public async Task<ReturnDayDto?> GetDayAsync(int passengerId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var choice = await _context.ReturnDayChoices
            .Include(c => c.Trip).ThenInclude(t => t!.Route)
            .Include(c => c.BoardingStop)
            .FirstOrDefaultAsync(c => c.PassengerId == passengerId && c.Date == date, cancellationToken);

        if (choice is null)
            return null;

        return BuildDayDto(date, choice, template: null, today: _clock.LocalToday);
    }

    public async Task<ReturnDayDto> UpsertDayAsync(int passengerId, DateOnly date, UpsertReturnDayDto dto, CancellationToken cancellationToken = default)
    {
        var today = _clock.LocalToday;
        if (date < today)
            throw new InvalidOperationException("Geçmiş bir gün için dönüş kararı değiştirilemez.");

        var passenger = await _context.Users.FirstOrDefaultAsync(u => u.Id == passengerId, cancellationToken)
            ?? throw new KeyNotFoundException("Yolcu bulunamadı.");

        Trip? trip = null;
        int? boardingStopId = null;

        if (dto.Decision == ReturnDecision.Coming)
        {
            if (dto.TripId <= 0)
                throw new InvalidOperationException("Dönüş için bir sefer seçilmelidir.");

            trip = await _context.Trips
                .Include(t => t.Route).ThenInclude(r => r.Stops)
                .FirstOrDefaultAsync(t => t.Id == dto.TripId && t.IsActive, cancellationToken)
                ?? throw new KeyNotFoundException("Sefer bulunamadı.");

            if (trip.Direction != TripDirection.Return)
                throw new InvalidOperationException("Seçilen sefer bir dönüş seferi değil.");

            if (trip.Route.CompanyId != passenger.CompanyId)
                throw new InvalidOperationException("Bu sefer şirketinize ait değil.");

            if (dto.BoardingStopId is > 0)
            {
                var stopBelongsToRoute = trip.Route.Stops.Any(s => s.Id == dto.BoardingStopId && s.IsActive);
                if (!stopBelongsToRoute)
                    throw new InvalidOperationException("Belirtilen durak bu sefere ait değil.");

                boardingStopId = dto.BoardingStopId;
            }
        }

        var existing = await _context.ReturnDayChoices
            .FirstOrDefaultAsync(c => c.PassengerId == passengerId && c.Date == date, cancellationToken);

        if (existing is null)
        {
            var inserted = new ReturnDayChoice
            {
                PassengerId = passengerId,
                Date = date,
                Decision = dto.Decision,
                TripId = trip!.Id,
                BoardingStopId = boardingStopId,
            };
            _context.ReturnDayChoices.Add(inserted);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                existing = inserted;
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                // Aynı gün için eşzamanlı ikinci istek (ör. butona çift dokunuş) araya girdi.
                // Kısıt zaten doğru sonucu koruyor; kazananın satırını güncelleyip devam ediyoruz.
                _context.Entry(inserted).State = EntityState.Detached;

                existing = await _context.ReturnDayChoices
                    .FirstAsync(c => c.PassengerId == passengerId && c.Date == date, cancellationToken);

                ApplyDecision(existing, dto.Decision, trip?.Id, boardingStopId);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
        else
        {
            ApplyDecision(existing, dto.Decision, trip?.Id, boardingStopId);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Karar kaydı şablonu her zaman ezer, bu yüzden şablonu yeniden okumaya gerek yok.
        // DTO'yu takip edilmeyen bir kopyadan kuruyoruz; kayıtlı entity'nin navigation'larına
        // dokunmak change tracker'ı gereksiz yere kirletirdi.
        var snapshot = new ReturnDayChoice
        {
            Date = date,
            Decision = existing.Decision,
            TripId = existing.TripId,
            BoardingStopId = existing.BoardingStopId,
            Trip = trip,
            BoardingStop = boardingStopId is null
                ? null
                : trip?.Route.Stops.FirstOrDefault(s => s.Id == boardingStopId.Value),
        };

        return BuildDayDto(date, snapshot, template: null, today: today);
    }

    public async Task ClearDayAsync(int passengerId, DateOnly date, CancellationToken cancellationToken = default)
    {
        if (date < _clock.LocalToday)
            throw new InvalidOperationException("Geçmiş bir gün için dönüş kararı değiştirilemez.");

        var existing = await _context.ReturnDayChoices
            .FirstOrDefaultAsync(c => c.PassengerId == passengerId && c.Date == date, cancellationToken);

        if (existing is null) return;

        _context.ReturnDayChoices.Remove(existing);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static void ApplyDecision(ReturnDayChoice choice, ReturnDecision decision, int? tripId, int? boardingStopId)
    {
        choice.Decision = decision;
        choice.TripId = tripId;
        choice.BoardingStopId = boardingStopId;
        choice.UpdatedAt = DateTime.UtcNow;
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    private static ReturnDayDto BuildDayDto(DateOnly date, ReturnDayChoice? choice, MonthlyReservation? template, DateOnly today)
    {
        var resolution = ReturnAttendanceResolver.Resolve(date, choice, template);

        // Çözümleme hangi kaynağın kazandığını söyler; isimleri o kaynaktan okuyoruz.
        var trip = resolution.TripId is null
            ? null
            : resolution.IsOverride ? choice!.Trip : template!.Trip;

        var stop = resolution.BoardingStopId is null
            ? null
            : resolution.IsOverride ? choice!.BoardingStop : template!.BoardingStop;

        return new ReturnDayDto
        {
            Date = date,
            State = resolution.State,
            IsOverride = resolution.IsOverride,
            IsEditable = date >= today,
            TripId = resolution.TripId,
            RouteName = trip?.Route?.Name,
            DepartureTime = trip?.DepartureTime,
            BoardingStopId = resolution.BoardingStopId,
            BoardingStopName = stop?.Name,
        };
    }
}

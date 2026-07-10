using Microsoft.EntityFrameworkCore;
using ServisYolcu.Core.DTOs.Reservation;
using ServisYolcu.Core.Entities;
using ServisYolcu.Core.Enums;
using ServisYolcu.Core.Interfaces;
using ServisYolcu.Core.Utilities;
using ServisYolcu.Infrastructure.Data;

namespace ServisYolcu.Infrastructure.Services;

public class MonthlyReservationService : IMonthlyReservationService
{
    private readonly AppDbContext _context;

    public MonthlyReservationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<MonthlyReservationDto> CreateMonthlyReservationAsync(int passengerId, CreateMonthlyReservationDto dto)
    {
        if (dto.Month is < 1 or > 12)
            throw new InvalidOperationException("Ay 1-12 aralığında olmalıdır.");
        if (dto.Year is < 2000 or > 2100)
            throw new InvalidOperationException("Yıl geçersiz.");

        var passenger = await _context.Users.FirstOrDefaultAsync(u => u.Id == passengerId)
            ?? throw new KeyNotFoundException("Yolcu bulunamadı.");

        var trip = await _context.Trips
            .Include(t => t.Route).ThenInclude(r => r.Stops)
            .FirstOrDefaultAsync(t => t.Id == dto.TripId && t.IsActive)
            ?? throw new KeyNotFoundException("Sefer bulunamadı.");

        if (trip.Route.CompanyId != passenger.CompanyId)
            throw new InvalidOperationException("Bu sefer şirketinize ait değil.");

        if (trip.Direction != dto.Direction)
            throw new InvalidOperationException(dto.Direction == TripDirection.Return
                ? "Dönüş planı için bir dönüş seferi seçmelisiniz."
                : "Gidiş planı için bir gidiş seferi seçmelisiniz.");

        // Bir yolcunun bir ay için yalnızca tek bir dönüş şablonu olabilir; aksi hâlde o ayın
        // günlerinin hangi şablondan türetileceği belirsiz kalır. Veritabanındaki filtreli
        // unique index'e çarpmadan önce anlaşılır bir hata döndürüyoruz.
        if (dto.Direction == TripDirection.Return)
        {
            var templateExists = await _context.MonthlyReservations.AnyAsync(
                m => m.PassengerId == passengerId
                     && m.Direction == TripDirection.Return
                     && m.Year == dto.Year
                     && m.Month == dto.Month);

            if (templateExists)
                throw new InvalidOperationException("Bu ay için zaten bir dönüş planınız var.");
        }
        else
        {
            var planExists = await _context.MonthlyReservations.AnyAsync(
                m => m.PassengerId == passengerId
                     && m.Direction == TripDirection.Outbound
                     && m.Year == dto.Year
                     && m.Month == dto.Month
                     && m.TripId == dto.TripId);

            if (planExists)
                throw new InvalidOperationException("Bu sefer için bu ayda zaten bir planınız var.");
        }

        Stop? boardingStop = null;
        if (dto.BoardingStopId is > 0)
        {
            boardingStop = trip.Route.Stops.FirstOrDefault(s => s.Id == dto.BoardingStopId.Value && s.IsActive)
                ?? throw new InvalidOperationException("Belirtilen durak bu sefere ait değil.");
        }

        var maxDay = DateTime.DaysInMonth(dto.Year, dto.Month);
        if (dto.DaysOff.Any(d => d < 1 || d > maxDay))
            throw new InvalidOperationException($"Gün numaraları 1-{maxDay} aralığında olmalıdır.");

        var mr = new MonthlyReservation
        {
            Year = dto.Year,
            Month = dto.Month,
            TripId = dto.TripId,
            PassengerId = passengerId,
            Direction = dto.Direction,
            BoardingStopId = boardingStop?.Id,
            DaysOff = DayList.Format(dto.DaysOff),
        };

        _context.MonthlyReservations.Add(mr);
        await _context.SaveChangesAsync();

        return MapToDto(mr, trip, boardingStop);
    }

    public async Task<IEnumerable<MonthlyReservationDto>> GetPassengerMonthlyReservationsAsync(
        int passengerId, int? year = null, int? month = null, TripDirection? direction = null)
    {
        var query = _context.MonthlyReservations
            .Include(m => m.Trip).ThenInclude(t => t.Route)
            .Include(m => m.BoardingStop)
            .Where(m => m.PassengerId == passengerId);

        if (year.HasValue)
            query = query.Where(m => m.Year == year.Value);
        if (month.HasValue)
            query = query.Where(m => m.Month == month.Value);
        if (direction.HasValue)
            query = query.Where(m => m.Direction == direction.Value);

        // DaysOff'un ayrıştırılması SQL'e çevrilemez; önce entity'leri çekip bellekte map'liyoruz.
        var rows = await query.ToListAsync();
        return rows.Select(m => MapToDto(m, m.Trip, m.BoardingStop)).ToList();
    }

    public async Task UpdateDaysOffAsync(int passengerId, int id, List<int> daysOff)
    {
        var mr = await _context.MonthlyReservations.FirstOrDefaultAsync(m => m.Id == id && m.PassengerId == passengerId)
            ?? throw new KeyNotFoundException("Aylık rezervasyon bulunamadı.");

        var maxDay = DateTime.DaysInMonth(mr.Year, mr.Month);
        if (daysOff.Any(d => d < 1 || d > maxDay))
            throw new InvalidOperationException($"Gün numaraları 1-{maxDay} aralığında olmalıdır.");

        mr.DaysOff = DayList.Format(daysOff);
        await _context.SaveChangesAsync();
    }

    /// <remarks>
    /// Dönüş şablonu silinse bile yolcunun o aya ait açık günlük kararları (ReturnDayChoice)
    /// korunur — bunlar şablondan bağımsız, bilinçli kararlardır. Şablonsuz kalan günler
    /// otomatik olarak "gelmiyor" durumuna düşer.
    /// </remarks>
    public async Task DeleteMonthlyReservationAsync(int passengerId, int id)
    {
        var mr = await _context.MonthlyReservations.FirstOrDefaultAsync(m => m.Id == id && m.PassengerId == passengerId)
            ?? throw new KeyNotFoundException("Aylık rezervasyon bulunamadı.");

        _context.MonthlyReservations.Remove(mr);
        await _context.SaveChangesAsync();
    }

    private static MonthlyReservationDto MapToDto(MonthlyReservation mr, Trip trip, Stop? boardingStop) => new()
    {
        Id = mr.Id,
        Year = mr.Year,
        Month = mr.Month,
        TripId = mr.TripId,
        RouteName = trip.Route.Name,
        DepartureTime = trip.DepartureTime,
        Direction = mr.Direction,
        BoardingStopId = mr.BoardingStopId,
        BoardingStopName = boardingStop?.Name,
        DaysOff = DayList.Parse(mr.DaysOff),
        CreatedAt = mr.CreatedAt,
    };
}

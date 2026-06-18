using Microsoft.EntityFrameworkCore;
using ServisYolcu.Core.DTOs.Reservation;
using ServisYolcu.Core.Entities;
using ServisYolcu.Core.Interfaces;
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
        var trip = await _context.Trips.Include(t => t.Route).FirstOrDefaultAsync(t => t.Id == dto.TripId && t.IsActive)
            ?? throw new KeyNotFoundException("Sefer bulunamadı.");

        var mr = new MonthlyReservation
        {
            Year = dto.Year,
            Month = dto.Month,
            TripId = dto.TripId,
            PassengerId = passengerId,
            DaysOff = string.Join(',', dto.DaysOff.OrderBy(d => d)),
        };

        _context.MonthlyReservations.Add(mr);
        await _context.SaveChangesAsync();

        return new MonthlyReservationDto
        {
            Id = mr.Id,
            Year = mr.Year,
            Month = mr.Month,
            TripId = mr.TripId,
            RouteName = trip.Route.Name,
            DaysOff = ParseDaysOff(mr.DaysOff),
            CreatedAt = mr.CreatedAt
        };
    }

    public async Task<IEnumerable<MonthlyReservationDto>> GetPassengerMonthlyReservationsAsync(int passengerId, int? year = null, int? month = null)
    {
        var query = _context.MonthlyReservations
            .Include(m => m.Trip).ThenInclude(t => t.Route)
            .Where(m => m.PassengerId == passengerId)
            .AsQueryable();

        if (year.HasValue)
            query = query.Where(m => m.Year == year.Value);
        if (month.HasValue)
            query = query.Where(m => m.Month == month.Value);

        return await query
            .Select(m => new MonthlyReservationDto
            {
                Id = m.Id,
                Year = m.Year,
                Month = m.Month,
                TripId = m.TripId,
                RouteName = m.Trip.Route.Name,
                DaysOff = ParseDaysOff(m.DaysOff),
                CreatedAt = m.CreatedAt
            })
            .ToListAsync();
    }

    public async Task UpdateDaysOffAsync(int passengerId, int id, List<int> daysOff)
    {
        var mr = await _context.MonthlyReservations.FirstOrDefaultAsync(m => m.Id == id && m.PassengerId == passengerId)
            ?? throw new KeyNotFoundException("Aylık rezervasyon bulunamadı.");

        mr.DaysOff = string.Join(',', daysOff.OrderBy(d => d));
        await _context.SaveChangesAsync();
    }

    public async Task DeleteMonthlyReservationAsync(int passengerId, int id)
    {
        var mr = await _context.MonthlyReservations.FirstOrDefaultAsync(m => m.Id == id && m.PassengerId == passengerId)
            ?? throw new KeyNotFoundException("Aylık rezervasyon bulunamadı.");

        _context.MonthlyReservations.Remove(mr);
        await _context.SaveChangesAsync();
    }

    private static List<int> ParseDaysOff(string csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return new List<int>();
        return csv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                  .Select(s => int.TryParse(s, out var v) ? v : 0)
                  .Where(v => v > 0)
                  .ToList();
    }
}

using Microsoft.EntityFrameworkCore;
using ServisYolcu.Core.DTOs.Trip;
using ServisYolcu.Core.Entities;
using ServisYolcu.Core.Interfaces;
using ServisYolcu.Infrastructure.Data;

namespace ServisYolcu.Infrastructure.Services;

public class TripService : ITripService
{
    private readonly AppDbContext _context;

    public TripService(AppDbContext context)
    {
        _context = context;
    }

        public async Task<IEnumerable<TripDto>> GetAvailableTripsAsync(int companyId, int passengerId)
        {
            return await _context.Trips
                .Include(t => t.Route)
                .Include(t => t.Driver)
                .Where(t =>
                    t.IsActive &&
                    t.AvailableSeats > 0 &&
                    t.Route.CompanyId == companyId &&
                    !t.Reservations.Any(r => r.PassengerId == passengerId && r.Status != ReservationStatus.Cancelled))
                .Select(t => MapToDto(t))
                .ToListAsync();
        }

    public async Task<TripDto?> GetTripByIdAsync(int id)
    {
        var trip = await _context.Trips
            .Include(t => t.Route)
            .Include(t => t.Driver)
            .FirstOrDefaultAsync(t => t.Id == id);

        return trip is null ? null : MapToDto(trip);
    }

    public async Task<TripDetailDto?> GetTripDetailAsync(int id, DateTime? date = null)
    {
        var trip = await _context.Trips
            .Include(t => t.Route)
                .ThenInclude(r => r.Stops.Where(s => s.IsActive).OrderBy(s => s.Order))
            .Include(t => t.Driver)
            .Include(t => t.Reservations.Where(r => r.Status != ReservationStatus.Cancelled))
                .ThenInclude(r => r.Passenger)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (trip is null) return null;

        var reservationsByStop = trip.Reservations
            .Where(r => r.BoardingStopId.HasValue)
            .GroupBy(r => r.BoardingStopId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        // include monthly subscribers for the queried date (use provided date or today)
        var queryDate = date?.Date ?? DateTime.UtcNow.Date;
        var day = queryDate.Day;
        var monthly = await _context.MonthlyReservations
            .Include(m => m.Passenger)
            .Where(m => m.TripId == trip.Id && m.Year == queryDate.Year && m.Month == queryDate.Month)
            .ToListAsync();

        var monthlyByPassenger = monthly
            .GroupBy(m => m.PassengerId)
            .ToDictionary(g => g.Key, g => g.First());

        var unassigned = trip.Reservations
            .Where(r => !r.BoardingStopId.HasValue)
            .Select(r => new PassengerInfoDto
            {
                ReservationId = r.Id,
                PassengerId   = r.PassengerId,
                FullName      = $"{r.Passenger.FirstName} {r.Passenger.LastName}",
                PhoneNumber   = r.Passenger.PhoneNumber,
                SeatCount     = r.SeatCount,
                Status        = r.Status.ToString(),
                IsMonthly     = false,
                IsComing      = IsPassengerComing(r, monthlyByPassenger, day)
            }).ToList();

        // exclude passengers who already have a reservation to avoid duplicates
        var existingPassengerIds = trip.Reservations.Select(r => r.PassengerId).ToHashSet();

        // For the queried trip date, include monthly subscribers as coming or not coming
        var monthlyDtos = monthly
            .Where(m => !existingPassengerIds.Contains(m.PassengerId))
            .Select(m => new PassengerInfoDto
            {
                ReservationId = 0,
                PassengerId = m.PassengerId,
                FullName = m.Passenger is null ? string.Empty : $"{m.Passenger.FirstName} {m.Passenger.LastName}",
                PhoneNumber = m.Passenger?.PhoneNumber ?? string.Empty,
                SeatCount = 1,
                IsMonthly = true,
                IsComing = !ParseDaysOff(m.DaysOff).Contains(day),
                Status = !ParseDaysOff(m.DaysOff).Contains(day) ? "Monthly" : "Monthly-Off"
            }).ToList();

        unassigned.AddRange(monthlyDtos);

        var monthlyCount = monthlyDtos.Count(m => m.IsComing);

        return new TripDetailDto
        {
            Id                   = trip.Id,
            RouteId              = trip.RouteId,
            RouteName            = trip.Route.Name,
            StartPoint           = trip.Route.StartPoint,
            EndPoint             = trip.Route.EndPoint,
            PricePerSeat         = trip.Route.PricePerSeat,
            DepartureTime        = trip.DepartureTime,
            TotalSeats           = trip.TotalSeats,
            AvailableSeats       = trip.AvailableSeats - monthlyCount,
            VehiclePlate         = trip.VehiclePlate,
            DriverId             = trip.DriverId,
            DriverName           = $"{trip.Driver.FirstName} {trip.Driver.LastName}",
            UnassignedPassengers = unassigned,
            Stops                = trip.Route.Stops
                                       .Select(s => new StopDetailDto
                                       {
                                           Id         = s.Id,
                                           Name       = s.Name,
                                           Address    = s.Address,
                                           Latitude   = s.Latitude,
                                           Longitude  = s.Longitude,
                                           Order      = s.Order,
                                           Passengers = reservationsByStop.TryGetValue(s.Id, out var resvs)
                                                          ? resvs.Select(r => new PassengerInfoDto
                                                            {
                                                                ReservationId = r.Id,
                                                                PassengerId   = r.PassengerId,
                                                                FullName      = $"{r.Passenger.FirstName} {r.Passenger.LastName}",
                                                                PhoneNumber   = r.Passenger.PhoneNumber,
                                                                SeatCount     = r.SeatCount,
                                                                                                                                Status        = r.Status.ToString(),
                                                                                                                                IsMonthly     = false,
                                                                                                                                IsComing      = IsPassengerComing(r, monthlyByPassenger, day)
                                                            }).ToList()
                                                          : new List<PassengerInfoDto>()
                                       }).ToList()
        };
    }

    public async Task<TripDto> CreateTripAsync(int driverId, CreateTripDto dto)
    {
        var route = await _context.Routes.FindAsync(dto.RouteId)
            ?? throw new KeyNotFoundException("Güzergah bulunamadı.");

        var trip = new Trip
        {
            RouteId = dto.RouteId,
            DriverId = driverId,
            DepartureTime = dto.DepartureTime,
            TotalSeats = dto.TotalSeats,
            AvailableSeats = dto.TotalSeats,
            VehiclePlate = dto.VehiclePlate
        };

        _context.Trips.Add(trip);
        await _context.SaveChangesAsync();

        await _context.Entry(trip).Reference(t => t.Route).LoadAsync();
        await _context.Entry(trip).Reference(t => t.Driver).LoadAsync();

        return MapToDto(trip);
    }

    public async Task<IEnumerable<TripDto>> GetDriverTripsAsync(int driverId)
    {
        return await _context.Trips
            .Include(t => t.Route)
            .Include(t => t.Driver)
            .Where(t => t.DriverId == driverId)
            .Select(t => MapToDto(t))
            .ToListAsync();
    }

    public async Task<ReservationDto> CreateReservationAsync(int passengerId, CreateReservationDto dto)
    {
        var trip = await _context.Trips
            .Include(t => t.Route)
                .ThenInclude(r => r.Stops)
            .FirstOrDefaultAsync(t => t.Id == dto.TripId && t.IsActive)
            ?? throw new KeyNotFoundException("Sefer bulunamadı.");

        var stopBelongsToRoute = trip.Route.Stops.Any(s => s.Id == dto.BoardingStopId && s.IsActive);
        if (!stopBelongsToRoute)
            throw new InvalidOperationException("Belirtilen durak bu sefere ait değil.");

        if (trip.AvailableSeats < dto.SeatCount)
            throw new InvalidOperationException("Yeterli koltuk yok.");

        trip.AvailableSeats -= dto.SeatCount;

        var reservation = new Reservation
        {
            TripId = dto.TripId,
            PassengerId = passengerId,
            SeatCount = dto.SeatCount,
            Status = ReservationStatus.Confirmed,
            BoardingStopId = dto.BoardingStopId
        };

        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync();

        return new ReservationDto
        {
            Id = reservation.Id,
            TripId = trip.Id,
            RouteName = trip.Route.Name,
            DepartureTime = trip.DepartureTime,
            SeatCount = reservation.SeatCount,
            Status = reservation.Status.ToString(),
            CreatedAt = reservation.CreatedAt
        };
    }

    public async Task<IEnumerable<ReservationDto>> GetPassengerReservationsAsync(int passengerId)
    {
        return await _context.Reservations
            .Include(r => r.Trip).ThenInclude(t => t.Route)
            .Where(r => r.PassengerId == passengerId)
            .Select(r => new ReservationDto
            {
                Id = r.Id,
                TripId = r.TripId,
                RouteName = r.Trip.Route.Name,
                DepartureTime = r.Trip.DepartureTime,
                SeatCount = r.SeatCount,
                Status = r.Status.ToString(),
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();
    }

    public async Task CancelReservationAsync(int passengerId, int reservationId)
    {
        var reservation = await _context.Reservations
            .Include(r => r.Trip)
            .FirstOrDefaultAsync(r => r.Id == reservationId && r.PassengerId == passengerId)
            ?? throw new KeyNotFoundException("Rezervasyon bulunamadı.");

        if (reservation.Status == ReservationStatus.Cancelled)
            throw new InvalidOperationException("Rezervasyon zaten iptal edilmiş.");

        reservation.Status = ReservationStatus.Cancelled;
        reservation.Trip.AvailableSeats += reservation.SeatCount;
        await _context.SaveChangesAsync();
    }

    private static TripDto MapToDto(Trip t, int? effectiveAvailable = null) => new()
    {
        Id = t.Id,
        RouteId = t.RouteId,
        RouteName = t.Route.Name,
        StartPoint = t.Route.StartPoint,
        EndPoint = t.Route.EndPoint,
        DepartureTime = t.DepartureTime,
        TotalSeats = t.TotalSeats,
        AvailableSeats = effectiveAvailable ?? t.AvailableSeats,
        VehiclePlate = t.VehiclePlate,
        DriverId = t.DriverId,
        DriverName = $"{t.Driver.FirstName} {t.Driver.LastName}",
        PricePerSeat = t.Route.PricePerSeat
    };

    private static bool IsPassengerComing(Reservation reservation, IReadOnlyDictionary<int, MonthlyReservation> monthlyByPassenger, int day)
    {
        if (reservation.Status != ReservationStatus.Confirmed)
            return false;

        if (!monthlyByPassenger.TryGetValue(reservation.PassengerId, out var monthlyReservation))
            return true;

        return !ParseDaysOff(monthlyReservation.DaysOff).Contains(day);
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

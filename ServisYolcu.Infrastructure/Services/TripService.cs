using Microsoft.EntityFrameworkCore;
using ServisYolcu.Core.DTOs.Trip;
using ServisYolcu.Core.Entities;
using ServisYolcu.Core.Enums;
using ServisYolcu.Core.Interfaces;
using ServisYolcu.Core.Utilities;
using ServisYolcu.Infrastructure.Data;

namespace ServisYolcu.Infrastructure.Services;

public class TripService : ITripService
{
    private readonly AppDbContext _context;
    private readonly IAppClock _clock;

    public TripService(AppDbContext context, IAppClock clock)
    {
        _context = context;
        _clock = clock;
    }

    public async Task<IEnumerable<TripDto>> GetAvailableTripsAsync(int companyId, int passengerId, TripDirection? direction = null)
    {
        var query = _context.Trips
            .Include(t => t.Route)
            .Include(t => t.Driver)
            .Where(t => t.IsActive && t.Route.CompanyId == companyId);

        if (direction.HasValue)
            query = query.Where(t => t.Direction == direction.Value);

        // Gidiş seferlerinde yolcunun zaten aktif rezervasyonu olan sefer tekrar listelenmez.
        // Dönüş seferlerinde Reservation kavramı yoktur (koltuk günlük kararla tutulur),
        // bu yüzden o filtre uygulanmaz.
        query = query.Where(t =>
            t.Direction == TripDirection.Return ||
            (t.AvailableSeats > 0 &&
             !t.Reservations.Any(r => r.PassengerId == passengerId && r.Status != ReservationStatus.Cancelled)));

        // Önce entity'leri çek: MapToDto çevrilebilir bir ifade değil ve entity döndürmeyen
        // bir projeksiyonda Include'lar yok sayılabilir (Route/Driver null gelirdi).
        var trips = await query.ToListAsync();
        return trips.Select(t => MapToDto(t)).ToList();
    }

    public async Task<TripDto?> GetTripByIdAsync(int id)
    {
        var trip = await _context.Trips
            .Include(t => t.Route)
            .Include(t => t.Driver)
            .FirstOrDefaultAsync(t => t.Id == id);

        return trip is null ? null : MapToDto(trip);
    }

    public async Task<TripDetailDto?> GetTripDetailAsync(int id, DateOnly? date = null)
    {
        var trip = await _context.Trips
            .Include(t => t.Route)
                .ThenInclude(r => r.Stops.Where(s => s.IsActive).OrderBy(s => s.Order))
            .Include(t => t.Driver)
            .Include(t => t.Reservations.Where(r => r.Status != ReservationStatus.Cancelled))
                .ThenInclude(r => r.Passenger)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (trip is null) return null;

        // Gün sınırı yerel takvime göre; UtcNow.Date gece yarısı civarında bir gün kaydırırdı.
        var queryDate = date ?? _clock.LocalToday;

        var roster = trip.Direction == TripDirection.Return
            ? await BuildReturnRosterAsync(trip, queryDate)
            : await BuildOutboundRosterAsync(trip, queryDate);

        return AssembleDetail(trip, queryDate, roster);
    }

    /// <summary>Bir yolcunun manifestodaki satırı ve hangi durakta gösterileceği.</summary>
    private sealed record RosterEntry(PassengerInfoDto Passenger, int? DisplayStopId);

    private async Task<List<RosterEntry>> BuildOutboundRosterAsync(Trip trip, DateOnly date)
    {
        var dayChoices = await _context.OutboundDayChoices
            .Include(c => c.Passenger)
            .Where(c => c.Date == date)
            .ToListAsync();

        var choiceByPassenger = dayChoices
            .GroupBy(c => c.PassengerId)
            .ToDictionary(g => g.Key, g => g.First());

        var monthly = await _context.MonthlyReservations
            .Include(m => m.Passenger)
            .Where(m => m.TripId == trip.Id
                        && m.Direction == TripDirection.Outbound
                        && m.Year == date.Year
                        && m.Month == date.Month)
            .ToListAsync();

        var monthlyByPassenger = monthly
            .GroupBy(m => m.PassengerId)
            .ToDictionary(g => g.Key, g => g.First());

        var entries = new List<RosterEntry>();

        foreach (var r in trip.Reservations)
        {
            choiceByPassenger.TryGetValue(r.PassengerId, out var overrideChoice);

            if (overrideChoice is not null && overrideChoice.TripId != trip.Id)
                continue;

            var coming = IsOutboundPassengerComing(r, monthlyByPassenger, date.Day);
            var isOverrideForThisTrip = overrideChoice is not null && overrideChoice.TripId == trip.Id;

            if (isOverrideForThisTrip)
                coming = true;

            entries.Add(new RosterEntry(
                new PassengerInfoDto
                {
                    ReservationId = r.Id,
                    PassengerId = r.PassengerId,
                    FullName = $"{r.Passenger.FirstName} {r.Passenger.LastName}",
                    PhoneNumber = r.Passenger.PhoneNumber,
                    SeatCount = r.SeatCount,
                    IsMonthly = false,
                    IsComing = coming,
                    State = coming ? ReturnAttendanceState.Confirmed : ReturnAttendanceState.NotComing,
                    Status = isOverrideForThisTrip
                        ? "Daily-Override"
                        : r.Status.ToString(),
                },
                isOverrideForThisTrip
                    ? overrideChoice!.BoardingStopId ?? r.BoardingStopId
                    : r.BoardingStopId));
        }

        // Rezervasyonu olan yolcuları iki kez listelememek için hariç tut.
        var existingPassengerIds = entries.Select(e => e.Passenger.PassengerId).ToHashSet();

        foreach (var m in monthly.Where(m => !existingPassengerIds.Contains(m.PassengerId)))
        {
            choiceByPassenger.TryGetValue(m.PassengerId, out var overrideChoice);

            if (overrideChoice is not null && overrideChoice.TripId != trip.Id)
                continue;

            var coming = !DayList.Parse(m.DaysOff).Contains(date.Day);
            var isOverrideForThisTrip = overrideChoice is not null && overrideChoice.TripId == trip.Id;

            if (isOverrideForThisTrip)
                coming = true;

            entries.Add(new RosterEntry(
                new PassengerInfoDto
                {
                    ReservationId = 0,
                    PassengerId = m.PassengerId,
                    FullName = m.Passenger is null ? string.Empty : $"{m.Passenger.FirstName} {m.Passenger.LastName}",
                    PhoneNumber = m.Passenger?.PhoneNumber ?? string.Empty,
                    SeatCount = 1,
                    IsMonthly = true,
                    IsComing = coming,
                    State = coming ? ReturnAttendanceState.Confirmed : ReturnAttendanceState.NotComing,
                    Status = isOverrideForThisTrip
                        ? "Daily-Override"
                        : (coming ? "Monthly" : "Monthly-Off"),
                },
                isOverrideForThisTrip
                    ? overrideChoice!.BoardingStopId ?? m.BoardingStopId
                    : m.BoardingStopId));
        }

        existingPassengerIds = entries.Select(e => e.Passenger.PassengerId).ToHashSet();

        var incomingOverrides = dayChoices
            .Where(c => c.TripId == trip.Id && !existingPassengerIds.Contains(c.PassengerId));

        foreach (var incoming in incomingOverrides)
        {
            entries.Add(new RosterEntry(
                new PassengerInfoDto
                {
                    ReservationId = 0,
                    PassengerId = incoming.PassengerId,
                    FullName = incoming.Passenger is null
                        ? string.Empty
                        : $"{incoming.Passenger.FirstName} {incoming.Passenger.LastName}",
                    PhoneNumber = incoming.Passenger?.PhoneNumber ?? string.Empty,
                    SeatCount = 1,
                    IsMonthly = false,
                    IsComing = true,
                    State = ReturnAttendanceState.Confirmed,
                    Status = "Daily-Override",
                },
                incoming.BoardingStopId));
        }

        return entries;
    }

    private async Task<List<RosterEntry>> BuildReturnRosterAsync(Trip trip, DateOnly date)
    {
        var rows = await ReturnRoster.BuildAsync(_context, trip.Id, date);

        // Şablondaki durak bu seferin rotasına ait değilse (rota sonradan düzenlenmişse)
        // yolcuyu duraksız listele; aksi hâlde manifestoda hiç görünmezdi.
        var stopIds = trip.Route.Stops.Select(s => s.Id).ToHashSet();

        return rows.Select(row =>
        {
            var displayStopId = row.BoardingStopId is not null && stopIds.Contains(row.BoardingStopId.Value)
                ? row.BoardingStopId
                : null;

            return new RosterEntry(
                new PassengerInfoDto
                {
                    ReservationId = 0,
                    PassengerId = row.PassengerId,
                    FullName = row.Passenger is null ? string.Empty : $"{row.Passenger.FirstName} {row.Passenger.LastName}",
                    PhoneNumber = row.Passenger?.PhoneNumber ?? string.Empty,
                    SeatCount = 1,
                    IsMonthly = row.HasTemplate,
                    IsComing = row.State != ReturnAttendanceState.NotComing,
                    State = row.State,
                    Status = row.State.ToString(),
                },
                displayStopId);
        }).ToList();
    }

    private static TripDetailDto AssembleDetail(Trip trip, DateOnly date, List<RosterEntry> roster)
    {
        var byStop = roster
            .Where(e => e.DisplayStopId.HasValue)
            .GroupBy(e => e.DisplayStopId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Passenger).ToList());

        var unassigned = roster
            .Where(e => !e.DisplayStopId.HasValue)
            .Select(e => e.Passenger)
            .ToList();

        var occupiedSeats = roster
            .Where(e => e.Passenger.IsComing)
            .Sum(e => Math.Max(1, e.Passenger.SeatCount));

        var availableSeats = trip.TotalSeats - occupiedSeats;

        return new TripDetailDto
        {
            Id = trip.Id,
            RouteId = trip.RouteId,
            RouteName = trip.Route.Name,
            StartPoint = trip.Route.StartPoint,
            EndPoint = trip.Route.EndPoint,
            PricePerSeat = trip.Route.PricePerSeat,
            DepartureTime = trip.DepartureTime,
            TotalSeats = trip.TotalSeats,
            AvailableSeats = Math.Max(0, availableSeats),
            VehiclePlate = trip.VehiclePlate,
            DriverId = trip.DriverId,
            DriverName = $"{trip.Driver.FirstName} {trip.Driver.LastName}",
            Direction = trip.Direction,
            Date = date,
            UnassignedPassengers = unassigned,
            Stops = trip.Route.Stops
                .Select(s => new StopDetailDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Address = s.Address,
                    Latitude = s.Latitude,
                    Longitude = s.Longitude,
                    Order = s.Order,
                    Passengers = byStop.TryGetValue(s.Id, out var list) ? list : new List<PassengerInfoDto>(),
                })
                .ToList(),
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
            VehiclePlate = dto.VehiclePlate,
            // Dönüş rotasından üretilen seferlerin yönü her zaman Return olmalı.
            Direction = route.IsReverse ? TripDirection.Return : dto.Direction,
        };

        _context.Trips.Add(trip);
        await _context.SaveChangesAsync();

        await _context.Entry(trip).Reference(t => t.Route).LoadAsync();
        await _context.Entry(trip).Reference(t => t.Driver).LoadAsync();

        return MapToDto(trip);
    }

    public async Task<IEnumerable<TripDto>> GetDriverTripsAsync(int driverId, TripDirection? direction = null)
    {
        var query = _context.Trips
            .Include(t => t.Route)
            .Include(t => t.Driver)
            .Where(t => t.DriverId == driverId);

        if (direction.HasValue)
            query = query.Where(t => t.Direction == direction.Value);

        var trips = await query.ToListAsync();
        return trips.Select(t => MapToDto(t)).ToList();
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

    public async Task<OutboundDayChoiceDto?> GetOutboundDayChoiceAsync(int passengerId, DateOnly date)
    {
        var choice = await _context.OutboundDayChoices
            .Include(c => c.Trip).ThenInclude(t => t.Route)
            .Include(c => c.BoardingStop)
            .FirstOrDefaultAsync(c => c.PassengerId == passengerId && c.Date == date);

        if (choice is null)
            return null;

        return new OutboundDayChoiceDto
        {
            Date = choice.Date,
            TripId = choice.TripId,
            RouteName = choice.Trip.Route.Name,
            DepartureTime = choice.Trip.DepartureTime,
            BoardingStopId = choice.BoardingStopId,
            BoardingStopName = choice.BoardingStop?.Name,
            IsEditable = date >= _clock.LocalToday,
        };
    }

    public async Task<OutboundDayChoiceDto> UpsertOutboundDayChoiceAsync(int passengerId, UpsertOutboundDayChoiceDto dto)
    {
        if (dto.Date < _clock.LocalToday)
            throw new InvalidOperationException("Geçmiş bir gün için günlük rota seçimi değiştirilemez.");

        var passenger = await _context.Users.FirstOrDefaultAsync(u => u.Id == passengerId)
            ?? throw new KeyNotFoundException("Yolcu bulunamadı.");

        var trip = await _context.Trips
            .Include(t => t.Route)
                .ThenInclude(r => r.Stops)
            .FirstOrDefaultAsync(t => t.Id == dto.TripId && t.IsActive)
            ?? throw new KeyNotFoundException("Sefer bulunamadı.");

        if (trip.Direction != TripDirection.Outbound)
            throw new InvalidOperationException("Günlük rota seçimi için bir gidiş seferi seçmelisiniz.");

        if (trip.Route.CompanyId != passenger.CompanyId)
            throw new InvalidOperationException("Bu sefer şirketinize ait değil.");

        if (dto.BoardingStopId is > 0)
        {
            var stopBelongsToRoute = trip.Route.Stops.Any(s => s.Id == dto.BoardingStopId.Value && s.IsActive);
            if (!stopBelongsToRoute)
                throw new InvalidOperationException("Belirtilen durak bu sefere ait değil.");
        }

        var choice = await _context.OutboundDayChoices
            .FirstOrDefaultAsync(c => c.PassengerId == passengerId && c.Date == dto.Date);

        if (choice is null)
        {
            choice = new OutboundDayChoice
            {
                PassengerId = passengerId,
                Date = dto.Date,
                TripId = trip.Id,
                BoardingStopId = dto.BoardingStopId,
            };
            _context.OutboundDayChoices.Add(choice);
        }
        else
        {
            choice.TripId = trip.Id;
            choice.BoardingStopId = dto.BoardingStopId;
            choice.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        var stop = dto.BoardingStopId is > 0
            ? trip.Route.Stops.FirstOrDefault(s => s.Id == dto.BoardingStopId.Value)
            : null;

        return new OutboundDayChoiceDto
        {
            Date = dto.Date,
            TripId = trip.Id,
            RouteName = trip.Route.Name,
            DepartureTime = trip.DepartureTime,
            BoardingStopId = dto.BoardingStopId,
            BoardingStopName = stop?.Name,
            IsEditable = true,
        };
    }

    public async Task ClearOutboundDayChoiceAsync(int passengerId, DateOnly date)
    {
        if (date < _clock.LocalToday)
            throw new InvalidOperationException("Geçmiş bir gün için günlük rota seçimi değiştirilemez.");

        var choice = await _context.OutboundDayChoices
            .FirstOrDefaultAsync(c => c.PassengerId == passengerId && c.Date == date);

        if (choice is null)
            return;

        _context.OutboundDayChoices.Remove(choice);
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
        PricePerSeat = t.Route.PricePerSeat,
        Direction = t.Direction,
    };

    private static bool IsOutboundPassengerComing(Reservation reservation, IReadOnlyDictionary<int, MonthlyReservation> monthlyByPassenger, int day)
    {
        if (reservation.Status != ReservationStatus.Confirmed)
            return false;

        if (!monthlyByPassenger.TryGetValue(reservation.PassengerId, out var monthlyReservation))
            return true;

        return !DayList.Parse(monthlyReservation.DaysOff).Contains(day);
    }
}

using ServisYolcu.Core.DTOs.Trip;
using ServisYolcu.Core.Enums;

namespace ServisYolcu.Core.Interfaces;

public interface ITripService
{
    Task<IEnumerable<TripDto>> GetAvailableTripsAsync(int companyId, int passengerId, TripDirection? direction = null);
    Task<TripDto?> GetTripByIdAsync(int id);

    /// <summary>
    /// Sürücü manifestosu. Gidiş seferlerinde roster Reservation + gidiş aboneliklerinden,
    /// dönüş seferlerinde dönüş şablonu + günlük kararlardan kurulur.
    /// </summary>
    Task<TripDetailDto?> GetTripDetailAsync(int id, DateOnly? date = null);

    Task<TripDto> CreateTripAsync(int driverId, CreateTripDto dto);
    Task<IEnumerable<TripDto>> GetDriverTripsAsync(int driverId, TripDirection? direction = null);
    Task<ReservationDto> CreateReservationAsync(int passengerId, CreateReservationDto dto);
    Task<IEnumerable<ReservationDto>> GetPassengerReservationsAsync(int passengerId);
    Task CancelReservationAsync(int passengerId, int reservationId);
}

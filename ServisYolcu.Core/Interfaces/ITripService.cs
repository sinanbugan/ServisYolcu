using ServisYolcu.Core.DTOs.Trip;

namespace ServisYolcu.Core.Interfaces;

public interface ITripService
{
    Task<IEnumerable<TripDto>> GetAvailableTripsAsync(int companyId, int passengerId);
    Task<TripDto?> GetTripByIdAsync(int id);
    Task<TripDetailDto?> GetTripDetailAsync(int id, DateTime? date = null);
    Task<TripDto> CreateTripAsync(int driverId, CreateTripDto dto);
    Task<IEnumerable<TripDto>> GetDriverTripsAsync(int driverId);
    Task<ReservationDto> CreateReservationAsync(int passengerId, CreateReservationDto dto);
    Task<IEnumerable<ReservationDto>> GetPassengerReservationsAsync(int passengerId);
    Task CancelReservationAsync(int passengerId, int reservationId);
}

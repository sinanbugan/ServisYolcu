using ServisYolcu.Core.DTOs.Reservation;
using ServisYolcu.Core.Enums;

namespace ServisYolcu.Core.Interfaces;

public interface IMonthlyReservationService
{
    Task<MonthlyReservationDto> CreateMonthlyReservationAsync(int passengerId, CreateMonthlyReservationDto dto);
    Task<IEnumerable<MonthlyReservationDto>> GetPassengerMonthlyReservationsAsync(int passengerId, int? year = null, int? month = null, TripDirection? direction = null);
    Task UpdateDaysOffAsync(int passengerId, int id, List<int> daysOff);
    Task DeleteMonthlyReservationAsync(int passengerId, int id);
}

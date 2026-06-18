using ServisYolcu.Core.DTOs.Reservation;

namespace ServisYolcu.Core.Interfaces;

public interface IMonthlyReservationService
{
    Task<MonthlyReservationDto> CreateMonthlyReservationAsync(int passengerId, CreateMonthlyReservationDto dto);
    Task<IEnumerable<MonthlyReservationDto>> GetPassengerMonthlyReservationsAsync(int passengerId, int? year = null, int? month = null);
    Task UpdateDaysOffAsync(int passengerId, int id, List<int> daysOff);
    Task DeleteMonthlyReservationAsync(int passengerId, int id);
}

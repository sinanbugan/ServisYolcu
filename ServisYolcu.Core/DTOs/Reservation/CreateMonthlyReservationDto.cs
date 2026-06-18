using System.ComponentModel.DataAnnotations;
namespace ServisYolcu.Core.DTOs.Reservation;

public class CreateMonthlyReservationDto
{
    [Required]
    public int Year { get; set; }

    [Required]
    [Range(1,12)]
    public int Month { get; set; }

    [Required]
    public int TripId { get; set; }

    // Days the passenger will NOT attend (1-31)
    public List<int> DaysOff { get; set; } = new();
}

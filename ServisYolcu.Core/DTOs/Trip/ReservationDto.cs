using System.ComponentModel.DataAnnotations;

namespace ServisYolcu.Core.DTOs.Trip;

public class CreateReservationDto
{
    [Required]
    public int TripId { get; set; }

    [Required]
    [Range(1, 10)]
    public int SeatCount { get; set; }

    public int? BoardingStopId { get; set; }
}

public class ReservationDto
{
    public int Id { get; set; }
    public int TripId { get; set; }
    public string RouteName { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
    public int SeatCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

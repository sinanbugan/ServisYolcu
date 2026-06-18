namespace ServisYolcu.Core.DTOs.Reservation;

public class MonthlyReservationDto
{
    public int Id { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public int TripId { get; set; }
    public string RouteName { get; set; } = string.Empty;
    public List<int> DaysOff { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

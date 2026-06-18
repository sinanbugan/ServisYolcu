using System.ComponentModel.DataAnnotations.Schema;

namespace ServisYolcu.Core.Entities;

public class MonthlyReservation
{
    public int Id { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }

    // comma-separated day numbers (e.g. "1,5,12") representing days the passenger will NOT attend
    public string DaysOff { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int TripId { get; set; }
    public Trip Trip { get; set; } = null!;

    public int PassengerId { get; set; }
    public User Passenger { get; set; } = null!;
}

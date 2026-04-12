namespace ServisYolcu.Core.Entities;

public class Trip
{
    public int Id { get; set; }
    public DateTime DepartureTime { get; set; }
    public int TotalSeats { get; set; }
    public int AvailableSeats { get; set; }
    public string? VehiclePlate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int RouteId { get; set; }
    public Route Route { get; set; } = null!;

    public int DriverId { get; set; }
    public User Driver { get; set; } = null!;

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}

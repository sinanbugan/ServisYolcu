namespace ServisYolcu.Core.Entities;

public class Route
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string StartPoint { get; set; } = string.Empty;
    public string EndPoint { get; set; } = string.Empty;
    public decimal PricePerSeat { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Trip> Trips { get; set; } = new List<Trip>();
    public ICollection<Stop> Stops { get; set; } = new List<Stop>();
}

namespace ServisYolcu.Core.Entities;

public enum ReservationStatus
{
    Pending,
    Confirmed,
    Cancelled
}

public class Reservation
{
    public int Id { get; set; }
    public int SeatCount { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int TripId { get; set; }
    public Trip Trip { get; set; } = null!;

    public int PassengerId { get; set; }
    public User Passenger { get; set; } = null!;

    public int? BoardingStopId { get; set; }
    public Stop? BoardingStop { get; set; }
}

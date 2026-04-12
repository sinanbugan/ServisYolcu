using System.ComponentModel.DataAnnotations;

namespace ServisYolcu.Core.DTOs.Trip;

public class CreateTripDto
{
    [Required]
    public int RouteId { get; set; }

    [Required]
    public DateTime DepartureTime { get; set; }

    [Required]
    [Range(1, 100)]
    public int TotalSeats { get; set; }

    [MaxLength(20)]
    public string? VehiclePlate { get; set; }
}

public class TripDto
{
    public int Id { get; set; }
    public int RouteId { get; set; }
    public string RouteName { get; set; } = string.Empty;
    public string StartPoint { get; set; } = string.Empty;
    public string EndPoint { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
    public int TotalSeats { get; set; }
    public int AvailableSeats { get; set; }
    public string? VehiclePlate { get; set; }
    public int DriverId { get; set; }
    public string DriverName { get; set; } = string.Empty;
    public decimal PricePerSeat { get; set; }
}

public class TripDetailDto
{
    public int Id { get; set; }
    public int RouteId { get; set; }
    public string RouteName { get; set; } = string.Empty;
    public string StartPoint { get; set; } = string.Empty;
    public string EndPoint { get; set; } = string.Empty;
    public decimal PricePerSeat { get; set; }
    public List<StopDetailDto> Stops { get; set; } = new();
    public List<PassengerInfoDto> UnassignedPassengers { get; set; } = new();
    public DateTime DepartureTime { get; set; }
    public int TotalSeats { get; set; }
    public int AvailableSeats { get; set; }
    public string? VehiclePlate { get; set; }
    public int DriverId { get; set; }
    public string DriverName { get; set; } = string.Empty;
}

public class PassengerInfoDto
{
    public int ReservationId { get; set; }
    public int PassengerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public int SeatCount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class StopDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int Order { get; set; }
    public List<PassengerInfoDto> Passengers { get; set; } = new();
}

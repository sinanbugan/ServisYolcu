using System.ComponentModel.DataAnnotations;

namespace ServisYolcu.Core.DTOs.Trip;

public class UpsertOutboundDayChoiceDto
{
    [Required]
    public DateOnly Date { get; set; }

    [Required]
    public int TripId { get; set; }

    public int? BoardingStopId { get; set; }
}

public class OutboundDayChoiceDto
{
    public DateOnly Date { get; set; }
    public int TripId { get; set; }
    public string RouteName { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
    public int? BoardingStopId { get; set; }
    public string? BoardingStopName { get; set; }
    public bool IsEditable { get; set; }
}
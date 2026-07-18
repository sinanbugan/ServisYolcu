using System.ComponentModel.DataAnnotations;
using ServisYolcu.Core.Enums;

namespace ServisYolcu.Core.DTOs.ReturnPlan;

/// <summary>Bir takvim günü için hesaplanmış etkin dönüş durumu.</summary>
public class ReturnDayDto
{
    public DateOnly Date { get; set; }
    public ReturnAttendanceState State { get; set; }

    /// <summary>true ise yolcu bu gün için açık bir karar kaydetti (şablon ezildi).</summary>
    public bool IsOverride { get; set; }

    /// <summary>Geçmiş günler artık değiştirilemez.</summary>
    public bool IsEditable { get; set; }

    public int? TripId { get; set; }
    public string? RouteName { get; set; }
    public DateTime? DepartureTime { get; set; }
    public int? BoardingStopId { get; set; }
    public string? BoardingStopName { get; set; }
}

public class UpsertReturnDayDto
{
    [Required]
    public ReturnDecision Decision { get; set; }

    /// <summary>Decision = Coming ise zorunlu; bir dönüş seferini göstermelidir.</summary>
    public int TripId { get; set; }

    /// <summary>Verilirse seferin rotasına ait aktif bir durak olmalıdır.</summary>
    public int BoardingStopId { get; set; }
}

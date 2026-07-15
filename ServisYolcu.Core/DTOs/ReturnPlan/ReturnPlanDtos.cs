using System.ComponentModel.DataAnnotations;
using ServisYolcu.Core.Enums;

namespace ServisYolcu.Core.DTOs.ReturnPlan;

/// <summary>Yolcunun bir aya ait dönüş şablonu (Direction = Return olan MonthlyReservation).</summary>
public class ReturnTemplateDto
{
    public int Id { get; set; }
    public int TripId { get; set; }
    public string RouteName { get; set; } = string.Empty;
    public string StartPoint { get; set; } = string.Empty;
    public string EndPoint { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
    public List<int> DaysOff { get; set; } = new();
    public int? BoardingStopId { get; set; }
    public string? BoardingStopName { get; set; }
}

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

/// <summary>Dönüş sekmesinin tek çağrıda ihtiyaç duyduğu her şey.</summary>
public class ReturnMonthDto
{
    public int Year { get; set; }
    public int Month { get; set; }

    /// <summary>Yolcunun bu ay için tanımlı şablonu; yoksa null.</summary>
    public ReturnTemplateDto? Template { get; set; }

    /// <summary>Ayın her günü için bir kayıt (1..ayın gün sayısı).</summary>
    public List<ReturnDayDto> Days { get; set; } = new();
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

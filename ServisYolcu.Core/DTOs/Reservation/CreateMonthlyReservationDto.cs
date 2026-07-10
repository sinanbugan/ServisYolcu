using System.ComponentModel.DataAnnotations;
using ServisYolcu.Core.Enums;

namespace ServisYolcu.Core.DTOs.Reservation;

public class CreateMonthlyReservationDto
{
    [Required]
    public int Year { get; set; }

    [Required]
    [Range(1,12)]
    public int Month { get; set; }

    [Required]
    public int TripId { get; set; }

    // Days the passenger will NOT attend (1-31)
    public List<int> DaysOff { get; set; } = new();

    /// <summary>
    /// Belirtilmezse gidiş aboneliği oluşturulur. Return için TripId bir dönüş seferi
    /// olmalıdır ve yolcunun o ay için başka bir dönüş şablonu bulunmamalıdır.
    /// </summary>
    public TripDirection Direction { get; set; } = TripDirection.Outbound;

    /// <summary>Dönüş şablonunun varsayılan biniş durağı. Seferin rotasına ait olmalıdır.</summary>
    public int? BoardingStopId { get; set; }
}

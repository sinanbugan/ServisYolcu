using ServisYolcu.Core.Enums;

namespace ServisYolcu.Core.Entities;

/// <summary>
/// Bir yolcunun bir ay için tek bir sefere aboneliği.
///
/// Direction = Outbound → gidiş aboneliği. DaysOff dışındaki her gün gelinir.
/// Direction = Return   → dönüş şablonu. "Normalde bu seferle dönerim, şu günler hariç."
/// Dönüş şablonu bir varsayımdır; günü kesinleştirmek için <see cref="ReturnDayChoice"/> yazılır.
///
/// Her iki yönde de DaysOff aynı anlamı taşır: "bu günlerde gelmeyeceğim".
/// </summary>
public class MonthlyReservation
{
    public int Id { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }

    // comma-separated day numbers (e.g. "1,5,12") representing days the passenger will NOT attend
    public string DaysOff { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Mevcut tüm satırlar Outbound'dur (migration varsayılanı).</summary>
    public TripDirection Direction { get; set; } = TripDirection.Outbound;

    /// <summary>
    /// Dönüş şablonunun varsayılan biniş durağı (iş yeri tarafı). Gidiş aboneliğinde
    /// biniş durağı hâlâ çıpa <see cref="Reservation"/> üzerinden gelir; bu alan null kalır.
    /// </summary>
    public int? BoardingStopId { get; set; }
    public Stop? BoardingStop { get; set; }

    public int TripId { get; set; }
    public Trip Trip { get; set; } = null!;

    public int PassengerId { get; set; }
    public User Passenger { get; set; } = null!;
}

using ServisYolcu.Core.Enums;

namespace ServisYolcu.Core.Entities;

/// <summary>
/// Yolcunun tek bir takvim günü için verdiği dönüş kararı. Aylık dönüş şablonunun
/// (Direction = Return olan MonthlyReservation) üzerine yazar.
///
/// Satırın varlığı = yolcu o gün için karar verdi. Satır yoksa gün, şablondan türetilir
/// ve sürücüye "Planlı" (onaylanmamış) olarak görünür.
///
/// Decision = Coming  → TripId zorunludur (aynı veya farklı bir dönüş seferi olabilir).
/// Decision = NotComing → TripId ve BoardingStopId null'dır.
/// </summary>
public class ReturnDayChoice
{
    public int Id { get; set; }

    /// <summary>Kararın ait olduğu takvim günü (yerel tarih; saat bileşeni yok).</summary>
    public DateOnly Date { get; set; }

    public ReturnDecision Decision { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public int PassengerId { get; set; }
    public User Passenger { get; set; } = null!;

    /// <summary>Coming ise seçilen dönüş seferi; NotComing ise null.</summary>
    public int? TripId { get; set; }
    public Trip? Trip { get; set; }

    /// <summary>Dönüşe binileceği durak (iş yeri tarafı). Seferin rotasına ait olmalıdır.</summary>
    public int? BoardingStopId { get; set; }
    public Stop? BoardingStop { get; set; }
}

namespace ServisYolcu.Core.Entities;

/// <summary>
/// Yolcunun tek bir takvim günü için verdiği gidiş seferi override kararı.
/// Kalıcı rezervasyon/aylık planı bozmaz; yalnızca seçilen gün için geçerlidir.
/// </summary>
public class OutboundDayChoice
{
    public int Id { get; set; }

    /// <summary>Kararın ait olduğu takvim günü (yerel tarih; saat bileşeni yok).</summary>
    public DateOnly Date { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public int PassengerId { get; set; }
    public User Passenger { get; set; } = null!;

    /// <summary>O gün için seçilen gidiş seferi.</summary>
    public int TripId { get; set; }
    public Trip Trip { get; set; } = null!;

    /// <summary>Verilirse seçilen seferin rotasına ait aktif bir durak olmalıdır.</summary>
    public int? BoardingStopId { get; set; }
    public Stop? BoardingStop { get; set; }
}
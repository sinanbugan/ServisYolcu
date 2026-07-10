namespace ServisYolcu.Core.Enums;

/// <summary>
/// Yolcunun belirli bir gün için kaydettiği dönüş kararı (ReturnDayChoice satırı).
/// Yalnızca yolcu aylık şablonundan saptığında veya günü açıkça onayladığında yazılır.
/// </summary>
public enum ReturnDecision
{
    NotComing = 0,
    Coming = 1
}

/// <summary>
/// Bir gün için hesaplanan etkin dönüş durumu. Aylık şablon ile günlük kararın
/// birleştirilmesiyle üretilir; veritabanında saklanmaz.
/// </summary>
public enum ReturnAttendanceState
{
    /// <summary>Yolcu o gün dönmüyor (açıkça reddetti, başka sefere geçti veya şablonda izinli).</summary>
    NotComing = 0,

    /// <summary>Aylık şablona göre bekleniyor ama yolcu henüz onaylamadı.</summary>
    Planned = 1,

    /// <summary>Yolcu o gün için bu seferi açıkça onayladı.</summary>
    Confirmed = 2
}

using ServisYolcu.Core.Entities;
using ServisYolcu.Core.Enums;

namespace ServisYolcu.Core.Utilities;

/// <summary>
/// Bir yolcunun belirli bir gündeki etkin dönüş durumunu, aylık şablon ile günlük
/// kararı birleştirerek çözer. Sürücü manifestosu (TripService) ve bildirim dağıtımı
/// (NotificationService) aynı fonksiyonu kullanır — iki yerin ayrışmaması için.
/// </summary>
public static class ReturnAttendanceResolver
{
    public readonly record struct Resolution(
        ReturnAttendanceState State,
        int? TripId,
        int? BoardingStopId,
        bool IsOverride);

    /// <param name="date">Çözümlenecek takvim günü.</param>
    /// <param name="choice">Yolcunun o güne ait kaydı (varsa). Hangi sefere işaret ettiği önemsizdir.</param>
    /// <param name="template">Yolcunun o aya ait dönüş şablonu (Direction = Return), varsa.</param>
    public static Resolution Resolve(DateOnly date, ReturnDayChoice? choice, MonthlyReservation? template)
    {
        // Günlük karar her zaman şablonu ezer.
        if (choice is not null)
        {
            return choice.Decision == ReturnDecision.Coming
                ? new Resolution(ReturnAttendanceState.Confirmed, choice.TripId, choice.BoardingStopId, true)
                : new Resolution(ReturnAttendanceState.NotComing, null, null, true);
        }

        // Şablon var ve gün izinli değilse: bekleniyor ama henüz onaylanmadı.
        if (template is not null && !DayList.Parse(template.DaysOff).Contains(date.Day))
            return new Resolution(ReturnAttendanceState.Planned, template.TripId, template.BoardingStopId, false);

        return new Resolution(ReturnAttendanceState.NotComing, null, null, false);
    }
}

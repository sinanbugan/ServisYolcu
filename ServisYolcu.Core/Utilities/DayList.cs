namespace ServisYolcu.Core.Utilities;

/// <summary>
/// MonthlyReservation.DaysOff gibi virgülle ayrılmış gün numarası listelerini okur/yazar.
/// Ayrıştırma mantığı daha önce TripService, MonthlyReservationService ve NotificationService
/// içinde üç ayrı kopya hâlindeydi; tek kaynak burasıdır.
/// </summary>
public static class DayList
{
    public static List<int> Parse(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return new List<int>();

        return csv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                  .Select(s => int.TryParse(s, out var value) ? value : 0)
                  .Where(v => v is > 0 and <= 31)
                  .Distinct()
                  .OrderBy(v => v)
                  .ToList();
    }

    public static string Format(IEnumerable<int> days) =>
        string.Join(',', days.Where(d => d is > 0 and <= 31).Distinct().OrderBy(d => d));
}

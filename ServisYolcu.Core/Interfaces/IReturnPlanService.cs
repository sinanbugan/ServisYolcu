using ServisYolcu.Core.DTOs.ReturnPlan;

namespace ServisYolcu.Core.Interfaces;

public interface IReturnPlanService
{
    /// <summary>Bir gün için yazılmış günlük dönüş kararını döner; yoksa null.</summary>
    Task<ReturnDayDto?> GetDayAsync(int passengerId, DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>Bir gün için yolcunun kararını yazar veya günceller (upsert).</summary>
    Task<ReturnDayDto> UpsertDayAsync(int passengerId, DateOnly date, UpsertReturnDayDto dto, CancellationToken cancellationToken = default);

    /// <summary>Günlük kararı siler; gün yeniden şablondan türetilir.</summary>
    Task ClearDayAsync(int passengerId, DateOnly date, CancellationToken cancellationToken = default);
}

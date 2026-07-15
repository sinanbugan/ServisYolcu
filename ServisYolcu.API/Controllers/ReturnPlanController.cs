using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServisYolcu.Core.DTOs.ReturnPlan;
using ServisYolcu.Core.Interfaces;

namespace ServisYolcu.API.Controllers;

/// <summary>
/// Yolcunun günlük dönüş kararları. Aylık dönüş şablonu MonthlyReservations üzerinden
/// (Direction = Return) yönetilir; burası o şablonun üzerine yazılan günlük kararlardır.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Passenger,Admin")]
public class ReturnPlanController : ControllerBase
{
    private readonly IReturnPlanService _service;

    public ReturnPlanController(IReturnPlanService service)
    {
        _service = service;
    }

    /// <summary>Bir ayın her günü için etkin dönüş durumu + o ayın şablonu.</summary>
    [HttpGet("month")]
    public async Task<ActionResult<ReturnMonthDto>> GetMonth(
        [FromQuery] int year, [FromQuery] int month, CancellationToken cancellationToken)
    {
        var passengerId = CurrentPassengerId();
        var result = await _service.GetMonthAsync(passengerId, year, month, cancellationToken);
        return Ok(result);
    }

    /// <summary>Bir gün için yazılmış günlük dönüş kararını döner; yoksa null.</summary>
    [HttpGet("days/{date}")]
    public async Task<ActionResult<ReturnDayDto?>> GetDay(DateOnly date, CancellationToken cancellationToken)
    {
        var passengerId = CurrentPassengerId();
        var day = await _service.GetDayAsync(passengerId, date, cancellationToken);
        return Ok(day);
    }

    /// <summary>Bir gün için dönüş kararını yazar veya günceller.</summary>
    [HttpPost("days/{date}")]
    public async Task<ActionResult<ReturnDayDto>> UpsertDay(
        DateOnly date, [FromBody] UpsertReturnDayDto dto, CancellationToken cancellationToken)
    {
        var passengerId = CurrentPassengerId();
        var day = await _service.UpsertDayAsync(passengerId, date, dto, cancellationToken);
        return Ok(day);
    }

    /// <summary>Günlük kararı siler; gün yeniden aylık şablondan türetilir.</summary>
    [HttpDelete("days/{date}")]
    public async Task<IActionResult> ClearDay(DateOnly date, CancellationToken cancellationToken)
    {
        var passengerId = CurrentPassengerId();
        await _service.ClearDayAsync(passengerId, date, cancellationToken);
        return NoContent();
    }
    private int CurrentPassengerId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

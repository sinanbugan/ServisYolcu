using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServisYolcu.Core.DTOs.Reservation;
using ServisYolcu.Core.Interfaces;

namespace ServisYolcu.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MonthlyReservationsController : ControllerBase
{
    private readonly IMonthlyReservationService _service;

    public MonthlyReservationsController(IMonthlyReservationService service)
    {
        _service = service;
    }

    [HttpPost]
    [Authorize(Roles = "Passenger,Admin")]
    public async Task<ActionResult<MonthlyReservationDto>> Create([FromBody] CreateMonthlyReservationDto dto)
    {
        var passengerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var mr = await _service.CreateMonthlyReservationAsync(passengerId, dto);
        return CreatedAtAction(nameof(GetMine), new { id = mr.Id }, mr);
    }

    [HttpGet("my-monthly")]
    [Authorize(Roles = "Passenger,Admin")]
    public async Task<ActionResult<IEnumerable<MonthlyReservationDto>>> GetMine([FromQuery] int? year, [FromQuery] int? month)
    {
        var passengerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var list = await _service.GetPassengerMonthlyReservationsAsync(passengerId, year, month);
        return Ok(list);
    }

    [HttpPut("{id}/days-off")]
    [Authorize(Roles = "Passenger,Admin")]
    public async Task<IActionResult> UpdateDaysOff(int id, [FromBody] List<int> daysOff)
    {
        var passengerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _service.UpdateDaysOffAsync(passengerId, id, daysOff);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Passenger,Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var passengerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _service.DeleteMonthlyReservationAsync(passengerId, id);
        return NoContent();
    }
}

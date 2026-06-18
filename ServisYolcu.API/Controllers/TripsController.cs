using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServisYolcu.Core.DTOs.Trip;
using ServisYolcu.Core.Interfaces;

namespace ServisYolcu.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TripsController : ControllerBase
{
    private readonly ITripService _tripService;

    public TripsController(ITripService tripService)
    {
        _tripService = tripService;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<TripDto>>> GetAvailable()
    {
        var companyId = int.Parse(User.FindFirstValue("CompanyId")!);
        var trips = await _tripService.GetAvailableTripsAsync(companyId);
        return Ok(trips);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TripDetailDto>> GetById(int id, [FromQuery] DateTime? date)
    {
        var trip = await _tripService.GetTripDetailAsync(id, date);
        if (trip is null) return NotFound();
        return Ok(trip);
    }

    [HttpPost]
    [Authorize(Roles = "Driver,Admin")]
    public async Task<ActionResult<TripDto>> Create([FromBody] CreateTripDto dto)
    {
        var driverId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var trip = await _tripService.CreateTripAsync(driverId, dto);
        return CreatedAtAction(nameof(GetById), new { id = trip.Id }, trip);
    }

    [HttpGet("my-trips")]
    [Authorize(Roles = "Driver,Admin")]
    public async Task<ActionResult<IEnumerable<TripDto>>> GetMyTrips()
    {
        var driverId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var trips = await _tripService.GetDriverTripsAsync(driverId);
        return Ok(trips);
    }

    [HttpPost("reservations")]
    [Authorize(Roles = "Passenger,Admin")]
    public async Task<ActionResult<ReservationDto>> CreateReservation([FromBody] CreateReservationDto dto)
    {
        var passengerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var reservation = await _tripService.CreateReservationAsync(passengerId, dto);
        return CreatedAtAction(nameof(GetById), new { id = reservation.TripId }, reservation);
    }

    [HttpGet("my-reservations")]
    [Authorize(Roles = "Passenger,Admin")]
    public async Task<ActionResult<IEnumerable<ReservationDto>>> GetMyReservations()
    {
        var passengerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var reservations = await _tripService.GetPassengerReservationsAsync(passengerId);
        return Ok(reservations);
    }

    [HttpDelete("reservations/{reservationId}")]
    [Authorize(Roles = "Passenger,Admin")]
    public async Task<IActionResult> CancelReservation(int reservationId)
    {
        var passengerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _tripService.CancelReservationAsync(passengerId, reservationId);
        return NoContent();
    }
}

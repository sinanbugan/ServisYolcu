using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServisYolcu.Core.DTOs.Route;
using ServisYolcu.Core.Interfaces;

namespace ServisYolcu.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoutesController : ControllerBase
{
    private readonly IRouteService _routeService;

    public RoutesController(IRouteService routeService)
    {
        _routeService = routeService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RouteDto>>> GetAll()
    {
        var routes = await _routeService.GetAllRoutesAsync();
        return Ok(routes);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RouteDto>> GetById(int id)
    {
        var route = await _routeService.GetRouteByIdAsync(id);
        if (route is null) return NotFound();
        return Ok(route);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<RouteDto>> Create([FromBody] CreateRouteDto dto)
    {
        var route = await _routeService.CreateRouteAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = route.Id }, route);
    }

    // --- Durak endpoint'leri ---

    [HttpPost("{routeId:int}/stops")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<StopDto>> AddStop(int routeId, [FromBody] UpsertStopDto dto)
    {
        var stop = await _routeService.AddStopAsync(routeId, dto);
        return CreatedAtAction(nameof(GetById), new { id = routeId }, stop);
    }

    [HttpPut("stops/{stopId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<StopDto>> UpdateStop(int stopId, [FromBody] UpsertStopDto dto)
    {
        var stop = await _routeService.UpdateStopAsync(stopId, dto);
        return Ok(stop);
    }

    [HttpDelete("stops/{stopId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteStop(int stopId)
    {
        await _routeService.DeleteStopAsync(stopId);
        return NoContent();
    }
}

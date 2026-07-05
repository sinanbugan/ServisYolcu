using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServisYolcu.Core.DTOs.Notification;
using ServisYolcu.Core.Interfaces;

namespace ServisYolcu.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpPost("devices")]
    public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceTokenDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        await _notificationService.RegisterDeviceTokenAsync(userId, dto.DeviceToken, dto.Platform);
        return Ok();
    }

    [HttpPost("personal")]
    public async Task<IActionResult> SendPersonal([FromBody] SendNotificationDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        var result = await _notificationService.SendPersonalNotificationAsync(userId, dto.Title, dto.Body, dto.Data);
        return Ok(result);
    }

    [HttpPost("multi")]
    public async Task<IActionResult> SendMulti([FromBody] SendNotificationDto dto, [FromQuery] int[] userIds)
    {
        var result = await _notificationService.SendMultiNotificationAsync(userIds, dto.Title, dto.Body, dto.Data);
        return Ok(result);
    }

    [HttpPost("bulk")]
    public async Task<IActionResult> SendBulk([FromBody] SendNotificationDto dto, [FromQuery] int? companyId)
    {
        var result = await _notificationService.SendBulkNotificationAsync(dto.Title, dto.Body, dto.Data, companyId);
        return Ok(result);
    }

    [HttpPost("trip-started")]
    public async Task<IActionResult> SendTripStarted([FromBody] TripStartedNotificationDto dto)
    {
        var result = await _notificationService.SendTripStartedNotificationAsync(dto.TripId, dto.Title, dto.Body, dto.Data);
        return Ok(result);
    }

    private int GetCurrentUserId()
    {
        var claimValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("nameid")?.Value;

        return int.TryParse(claimValue, out var userId) ? userId : 0;
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServisYolcu.Core.DTOs.Menu;
using ServisYolcu.Core.Enums;
using ServisYolcu.Core.Interfaces;

namespace ServisYolcu.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MenusController : ControllerBase
{
    private readonly IMenuService _menuService;

    public MenusController(IMenuService menuService)
    {
        _menuService = menuService;
    }

    /// <summary>
    /// Giriş yapan kullanıcının rolüne göre erişebileceği menüleri döner.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MenuDto>>> GetMyMenus()
    {
        var roleClaim = User.FindFirstValue(ClaimTypes.Role);
        if (!Enum.TryParse<UserRole>(roleClaim, out var role))
            return Forbid();

        var menus = await _menuService.GetMenusByRoleAsync(role);
        return Ok(menus);
    }
}

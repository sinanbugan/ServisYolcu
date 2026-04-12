using Microsoft.EntityFrameworkCore;
using ServisYolcu.Core.DTOs.Menu;
using ServisYolcu.Core.Entities;
using ServisYolcu.Core.Enums;
using ServisYolcu.Core.Interfaces;
using ServisYolcu.Infrastructure.Data;

namespace ServisYolcu.Infrastructure.Services;

public class MenuService : IMenuService
{
    private readonly AppDbContext _context;

    public MenuService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<MenuDto>> GetMenusByRoleAsync(UserRole role)
    {
        var menus = await _context.Menus
            .Where(m => m.IsActive
                     && m.ParentId == null
                     && m.MenuRoles.Any(mr => mr.Role == role))
            .Include(m => m.Children.Where(c => c.IsActive && c.MenuRoles.Any(mr => mr.Role == role)))
            .OrderBy(m => m.Order)
            .ToListAsync();

        return menus.Select(MapToDto);
    }

    private static MenuDto MapToDto(Menu menu) => new()
    {
        Id       = menu.Id,
        Key      = menu.Key,
        Label    = menu.Label,
        Icon     = menu.Icon,
        Path     = menu.Path,
        Order    = menu.Order,
        ParentId = menu.ParentId,
        Children = menu.Children
                       .OrderBy(c => c.Order)
                       .Select(MapToDto)
                       .ToList()
    };
}

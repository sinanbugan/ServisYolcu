using ServisYolcu.Core.DTOs.Menu;
using ServisYolcu.Core.Enums;

namespace ServisYolcu.Core.Interfaces;

public interface IMenuService
{
    Task<IEnumerable<MenuDto>> GetMenusByRoleAsync(UserRole role);
}

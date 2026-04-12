using ServisYolcu.Core.DTOs.User;
using ServisYolcu.Core.Enums;

namespace ServisYolcu.Core.Interfaces;

public interface IUserService
{
    Task<IEnumerable<UserDto>> GetAllUsersAsync();
    Task<UserDto?> GetUserByIdAsync(int id);
    Task<UserDto> UpdateUserRoleAsync(int id, UserRole role);
    Task DeactivateUserAsync(int id);
}

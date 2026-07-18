using ServisYolcu.Core.Entities;

namespace ServisYolcu.Core.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
}

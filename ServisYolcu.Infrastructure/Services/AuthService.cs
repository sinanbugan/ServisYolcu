using Microsoft.EntityFrameworkCore;
using ServisYolcu.Core.DTOs.Auth;
using ServisYolcu.Core.Entities;
using ServisYolcu.Core.Enums;
using ServisYolcu.Core.Interfaces;
using ServisYolcu.Infrastructure.Data;

namespace ServisYolcu.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly ITokenService _tokenService;

    public AuthService(AppDbContext context, ITokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    public async Task<TokenResponseDto> RegisterAsync(RegisterDto dto)
    {
        var emailExists = await _context.Users.AnyAsync(u => u.Email == dto.Email.ToLower());
        if (emailExists)
            throw new InvalidOperationException("Bu e-posta adresi zaten kullanılıyor.");

        Company? company = null;
        if (dto.Role == UserRole.Passenger)
        {
            if (string.IsNullOrEmpty(dto.CompanyCode))
                throw new InvalidOperationException("Yolcu kaydı için şirket kodu gereklidir.");

            company = await _context.Companies.FirstOrDefaultAsync(c => c.CompanyCode == dto.CompanyCode && c.IsActive);
            if (company == null)
                throw new InvalidOperationException("Geçersiz şirket kodu.");
        }
        else if (dto.Role == UserRole.Driver || dto.Role == UserRole.Admin)
        {
            // Admin veya Driver için varsayılan bir company oluştur veya mevcut birini kullan
            // Şimdilik basit tutalım, belki admin için ayrı logic
            company = await _context.Companies.FirstOrDefaultAsync(c => c.CompanyCode == "DEFAULT" && c.IsActive);
            if (company == null)
            {
                company = new Company
                {
                    Name = "Default Company",
                    CompanyCode = "DEFAULT"
                };
                _context.Companies.Add(company);
                await _context.SaveChangesAsync();
            }
        }

        var user = new User
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email.ToLower(),
            PhoneNumber = dto.PhoneNumber,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = dto.Role,
            CompanyId = company!.Id
        };

        if (dto.Role == UserRole.Driver)
        {
            user.RefNumber = GenerateRefNumber();
        }

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return await GenerateTokensAsync(user);
    }

    public async Task<TokenResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == dto.Email.ToLower() && u.IsActive);

        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("E-posta veya şifre hatalı.");

        return await GenerateTokensAsync(user);
    }

    public async Task<TokenResponseDto> RefreshTokenAsync(string refreshToken)
    {
        var token = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken && !rt.IsRevoked);

        if (token is null || token.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Geçersiz veya süresi dolmuş refresh token.");

        if (!token.User.IsActive)
            throw new UnauthorizedAccessException("Kullanıcı hesabı devre dışı.");

        token.IsRevoked = true;
        await _context.SaveChangesAsync();

        return await GenerateTokensAsync(token.User);
    }

    public async Task RevokeTokenAsync(string refreshToken)
    {
        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken && !rt.IsRevoked);

        if (token is null)
            throw new InvalidOperationException("Token bulunamadı.");

        token.IsRevoked = true;
        await _context.SaveChangesAsync();
    }

    private async Task<TokenResponseDto> GenerateTokensAsync(User user)
    {
        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshTokenValue = _tokenService.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            Token = refreshTokenValue,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return new TokenResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            AccessTokenExpiry = DateTime.UtcNow.AddMinutes(60)
        };
    }

    private string GenerateRefNumber()
    {
        return Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
    }
}

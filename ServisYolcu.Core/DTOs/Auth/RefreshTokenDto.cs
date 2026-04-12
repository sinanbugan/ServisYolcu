using System.ComponentModel.DataAnnotations;

namespace ServisYolcu.Core.DTOs.Auth;

public class RefreshTokenDto
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

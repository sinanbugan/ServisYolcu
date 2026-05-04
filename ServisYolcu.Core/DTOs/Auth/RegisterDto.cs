using System.ComponentModel.DataAnnotations;
using ServisYolcu.Core.Enums;

namespace ServisYolcu.Core.DTOs.Auth;

public class RegisterDto
{
    [Required]
    [MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; } = UserRole.Passenger;

    public string? CompanyCode { get; set; }
}

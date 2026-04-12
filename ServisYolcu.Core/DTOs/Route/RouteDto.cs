using System.ComponentModel.DataAnnotations;

namespace ServisYolcu.Core.DTOs.Route;

public class StopDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int Order { get; set; }
}

public class RouteDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string StartPoint { get; set; } = string.Empty;
    public string EndPoint { get; set; } = string.Empty;
    public decimal PricePerSeat { get; set; }
    public bool IsActive { get; set; }
    public List<StopDto> Stops { get; set; } = new();
}

public class CreateRouteDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string StartPoint { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string EndPoint { get; set; } = string.Empty;

    [Required]
    [Range(0.01, 10000)]
    public decimal PricePerSeat { get; set; }

    public List<CreateStopDto> Stops { get; set; } = new();
}

public class CreateStopDto
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(250)]
    public string? Address { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    [Required]
    [Range(1, 1000)]
    public int Order { get; set; }
}

public class UpsertStopDto
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(250)]
    public string? Address { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    [Required]
    [Range(1, 1000)]
    public int Order { get; set; }
}

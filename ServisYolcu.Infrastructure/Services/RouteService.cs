using Microsoft.EntityFrameworkCore;
using ServisYolcu.Core.DTOs.Route;
using ServisYolcu.Core.Entities;
using ServisYolcu.Core.Interfaces;
using ServisYolcu.Infrastructure.Data;

namespace ServisYolcu.Infrastructure.Services;

public class RouteService : IRouteService
{
    private readonly AppDbContext _context;

    public RouteService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<RouteDto>> GetAllRoutesAsync(int companyId)
    {
        var routes = await _context.Routes
            .Where(r => r.IsActive && r.CompanyId == companyId)
            .Include(r => r.Stops.Where(s => s.IsActive).OrderBy(s => s.Order))
            .ToListAsync();

        return routes.Select(MapToDto);
    }

    public async Task<RouteDto?> GetRouteByIdAsync(int id)
    {
        var route = await _context.Routes
            .Include(r => r.Stops.Where(s => s.IsActive).OrderBy(s => s.Order))
            .FirstOrDefaultAsync(r => r.Id == id);

        return route is null ? null : MapToDto(route);
    }

    public async Task<RouteDto> CreateRouteAsync(int companyId, CreateRouteDto dto)
    {
        var orderedStops = dto.Stops
            .OrderBy(s => s.Order)
            .ToList();

        var route = new Route
        {
            Name = dto.Name,
            StartPoint = dto.StartPoint,
            EndPoint = dto.EndPoint,
            PricePerSeat = dto.PricePerSeat,
            IsReverse = false,
            CompanyId = companyId,
            Stops = orderedStops.Select((s, index) => new Stop
            {
                Name = s.Name,
                Address = s.Address,
                Latitude = s.Latitude,
                Longitude = s.Longitude,
                Order = index + 1,
                CompanyId = companyId
            }).ToList()
        };

        var returnRoute = new Route
        {
            Name = $"{dto.Name} - Dönüş",
            StartPoint = dto.EndPoint,
            EndPoint = dto.StartPoint,
            PricePerSeat = dto.PricePerSeat,
            IsReverse = true,
            CompanyId = companyId,
            Stops = orderedStops
                .AsEnumerable()
                .Reverse()
                .Select((s, index) => new Stop
                {
                    Name = s.Name,
                    Address = s.Address,
                    Latitude = s.Latitude,
                    Longitude = s.Longitude,
                    Order = index + 1,
                    CompanyId = companyId
                })
                .ToList()
        };

        _context.Routes.AddRange(route, returnRoute);
        await _context.SaveChangesAsync();

        route.ReverseRouteId = returnRoute.Id;
        returnRoute.ReverseRouteId = route.Id;
        await _context.SaveChangesAsync();

        return MapToDto(route);
    }

    public async Task<StopDto> AddStopAsync(int routeId, UpsertStopDto dto)
    {
        var route = await _context.Routes.FindAsync(routeId)
            ?? throw new KeyNotFoundException($"Rota bulunamadı: {routeId}");

        var stop = new Stop
        {
            RouteId = routeId,
            Name = dto.Name,
            Address = dto.Address,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            Order = dto.Order,
            CompanyId = route.CompanyId
        };

        _context.Stops.Add(stop);
        await _context.SaveChangesAsync();
        return MapStopToDto(stop);
    }

    public async Task<StopDto> UpdateStopAsync(int stopId, UpsertStopDto dto)
    {
        var stop = await _context.Stops.FindAsync(stopId)
            ?? throw new KeyNotFoundException($"Durak bulunamadı: {stopId}");

        stop.Name = dto.Name;
        stop.Address = dto.Address;
        stop.Latitude = dto.Latitude;
        stop.Longitude = dto.Longitude;
        stop.Order = dto.Order;

        await _context.SaveChangesAsync();
        return MapStopToDto(stop);
    }

    public async Task DeleteStopAsync(int stopId)
    {
        var stop = await _context.Stops.FindAsync(stopId)
            ?? throw new KeyNotFoundException($"Durak bulunamadı: {stopId}");

        stop.IsActive = false;
        await _context.SaveChangesAsync();
    }

    private static RouteDto MapToDto(Route route) => new()
    {
        Id = route.Id,
        Name = route.Name,
        StartPoint = route.StartPoint,
        EndPoint = route.EndPoint,
        PricePerSeat = route.PricePerSeat,
        ReverseRouteId = route.ReverseRouteId,
        IsReverse = route.IsReverse,
        IsActive = route.IsActive,
        Stops = route.Stops.Select(MapStopToDto).ToList()
    };

    private static StopDto MapStopToDto(Stop stop) => new()
    {
        Id = stop.Id,
        Name = stop.Name,
        Address = stop.Address,
        Latitude = stop.Latitude,
        Longitude = stop.Longitude,
        Order = stop.Order
    };
}

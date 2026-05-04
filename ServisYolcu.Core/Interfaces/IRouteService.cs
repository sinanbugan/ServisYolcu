using ServisYolcu.Core.DTOs.Route;
using ServisYolcu.Core.Entities;

namespace ServisYolcu.Core.Interfaces;

public interface IRouteService
{
    Task<IEnumerable<RouteDto>> GetAllRoutesAsync(int companyId);
    Task<RouteDto?> GetRouteByIdAsync(int id);
    Task<RouteDto> CreateRouteAsync(int companyId, CreateRouteDto dto);
    Task<StopDto> AddStopAsync(int routeId, UpsertStopDto dto);
    Task<StopDto> UpdateStopAsync(int stopId, UpsertStopDto dto);
    Task DeleteStopAsync(int stopId);
}

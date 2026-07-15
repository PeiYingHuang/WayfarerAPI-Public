using WayfarerAPI.Application.DTOs;

namespace WayfarerAPI.Application.Interfaces.Service;

public interface IConfigIataService
{
    Task<ConfigIataDto?> GetAirportByIataCodeAsync(string iataCode);
}

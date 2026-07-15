using WayfarerAPI.Domain.Entities;

namespace WayfarerAPI.Application.Interfaces.Utilities;

public interface IIataGeoClient
{
    Task<ConfigIata?> GetAirportAsync(string iataCode);
}

using WayfarerAPI.Domain.Entities;

namespace WayfarerAPI.Application.Interfaces.Repositories;

public interface IConfigIataRepository
{
    Task<ConfigIata?> GetByIataCodeAsync(string iataCode);
    Task InsertAsync(ConfigIata entity);
}

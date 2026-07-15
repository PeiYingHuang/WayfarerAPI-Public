using WayfarerAPI.Domain.Entities;

namespace WayfarerAPI.Application.Interfaces.Repositories;

public interface ITravelRepository
{
    Task<Travel?> GetByIdAsync(Guid id);
    Task<IEnumerable<Travel>> GetByTravellerIdAsync(Guid travellerId);
    Task InsertAsync(Travel travel);
    Task UpdateAsync(Travel travel);
    Task DeleteAsync(Guid id);
}

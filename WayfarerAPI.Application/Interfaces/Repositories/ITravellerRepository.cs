using WayfarerAPI.Domain.Entities;

namespace WayfarerAPI.Application.Interfaces.Repositories;

public interface ITravellerRepository
{
    Task<Traveller?> GetByIdAsync(Guid userId);
    Task InsertAsync(Traveller traveller);
    Task UpdateAsync(Traveller traveller);
}

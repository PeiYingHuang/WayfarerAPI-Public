using WayfarerAPI.Domain.Entities;

namespace WayfarerAPI.Application.Interfaces.Repositories;

public interface IItineraryDetailRepository
{
    Task<IEnumerable<ItineraryDetail>> GetByItineraryIdAsync(Guid itineraryId);
    Task InsertAsync(ItineraryDetail detail);
    Task DeleteByItineraryIdAsync(Guid itineraryId);
}

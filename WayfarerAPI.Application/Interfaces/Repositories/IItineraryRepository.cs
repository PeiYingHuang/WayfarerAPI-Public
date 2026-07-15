using WayfarerAPI.Domain.Entities;

namespace WayfarerAPI.Application.Interfaces.Repositories;

public interface IItineraryRepository
{
    Task<Itinerary?> GetByTravelIdAndDateAsync(Guid travelId, DateTime date);
    Task<IEnumerable<Itinerary>> GetByTravelIdAsync(Guid travelId);
    Task InsertAsync(Itinerary itinerary);
    Task UpdateDayTitleAsync(Guid id, string? dayTitle);
}

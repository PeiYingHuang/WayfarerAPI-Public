using WayfarerAPI.Domain.Entities;

namespace WayfarerAPI.Application.Interfaces.Repositories;

public interface ITravelFlightRepository
{
    Task<IEnumerable<TravelFlight>> GetByTravelIdAsync(Guid travelId);
    Task InsertAsync(TravelFlight flight);
    Task DeleteByTravelIdAsync(Guid travelId);
}
